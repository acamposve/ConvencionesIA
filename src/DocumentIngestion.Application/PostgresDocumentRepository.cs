using System.Data;
using DocumentIngestion.Domain;
using Npgsql;

namespace DocumentIngestion.Application;

public sealed class PostgresDocumentRepository : IDocumentRepository
{
    private readonly string _connectionString;
    private readonly RepositoryOperationLogger? _logger;

    public PostgresDocumentRepository(string connectionString)
        : this(connectionString, null)
    {
    }

    public PostgresDocumentRepository(string connectionString, RepositoryOperationLogger? logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
        _logger = logger;
    }

    public void Save(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        try
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            SaveInternal(connection, transaction, document);
            transaction.Commit();
        }
        catch (Exception ex)
        {
            _logger?.LogRepositoryError("Save", ex);
            throw;
        }
    }

    public bool TryCreate(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        try
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            var existing = GetByTenantAndIdempotencyKeyInternal(connection, transaction, document.TenantId.Value, document.IdempotencyKey.Value);
            if (existing is not null)
            {
                transaction.Commit();
                return false;
            }

            SaveInternal(connection, transaction, document);
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogRepositoryError("TryCreate", ex);
            throw;
        }
    }

    public Document? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        try
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT document_id, tenant_id, document_key, source, source_reference, source_name, document_type, format, ingestion_state, current_outcome, current_processing_stage, correlation_id, idempotency_key, file_size_bytes, mime_type, language, page_count, author, creation_date, raw_text, normalized_text, rejection_reason, created_at, updated_at FROM documents WHERE document_id = @documentId";
            command.Parameters.AddWithValue("documentId", id);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapDocument(reader, connection, null);
        }
        catch (Exception ex)
        {
            _logger?.LogRepositoryError("GetById", ex);
            throw;
        }
    }

    public Document? GetByTenantAndIdempotencyKey(string tenantId, string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        using var connection = OpenConnection();
        return GetByTenantAndIdempotencyKeyInternal(connection, null, tenantId, idempotencyKey);
    }

    public IReadOnlyList<DocumentPersistenceContract> GetAll(string? tenantId, int page, int pageSize)
    {
        if (page <= 0)
        {
            page = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT document_id, tenant_id, document_key, source, source_reference, source_name, document_type, format, ingestion_state, current_outcome, current_processing_stage, correlation_id, idempotency_key, file_size_bytes, mime_type, language, page_count, author, creation_date, raw_text, normalized_text, rejection_reason, created_at, updated_at
            FROM documents
            WHERE (@tenantId IS NULL OR tenant_id = @tenantId)
            ORDER BY created_at DESC
            LIMIT @pageSize OFFSET @offset";
        command.Parameters.AddWithValue("tenantId", (object?)tenantId ?? DBNull.Value);
        command.Parameters.AddWithValue("pageSize", pageSize);
        command.Parameters.AddWithValue("offset", (page - 1) * pageSize);

        using var reader = command.ExecuteReader();
        var documents = new List<DocumentPersistenceContract>();
        while (reader.Read())
        {
            var document = MapDocument(reader, connection, null);
            documents.Add(ToPersistenceContract(document));
        }

        return documents;
    }

    private void InitializeSchema()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS tenants (
                tenant_id TEXT PRIMARY KEY,
                tenant_code TEXT NOT NULL UNIQUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS documents (
                document_id TEXT PRIMARY KEY,
                tenant_id TEXT NOT NULL,
                document_key TEXT NOT NULL UNIQUE,
                source TEXT NOT NULL,
                source_reference TEXT NOT NULL,
                source_name TEXT,
                document_type TEXT NOT NULL,
                format TEXT NOT NULL,
                ingestion_state TEXT NOT NULL,
                current_outcome TEXT NOT NULL,
                current_processing_stage TEXT NOT NULL,
                correlation_id TEXT,
                idempotency_key TEXT,
                file_size_bytes BIGINT NOT NULL,
                mime_type TEXT NOT NULL,
                language TEXT,
                page_count INTEGER,
                author TEXT,
                creation_date TIMESTAMPTZ,
                raw_text TEXT,
                normalized_text TEXT,
                rejection_reason TEXT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                FOREIGN KEY (tenant_id) REFERENCES tenants(tenant_id)
            );

            CREATE TABLE IF NOT EXISTS document_revisions (
                revision_id TEXT PRIMARY KEY,
                document_id TEXT NOT NULL,
                version INTEGER NOT NULL,
                revision_timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                outcome TEXT NOT NULL,
                processing_stage TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                FOREIGN KEY (document_id) REFERENCES documents(document_id) ON DELETE CASCADE,
                UNIQUE(document_id, version)
            );

            CREATE TABLE IF NOT EXISTS clauses (
                clause_id TEXT PRIMARY KEY,
                document_id TEXT NOT NULL,
                sequence INTEGER NOT NULL,
                number_label TEXT,
                text TEXT NOT NULL,
                span_start INTEGER NOT NULL,
                span_end INTEGER NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                FOREIGN KEY (document_id) REFERENCES documents(document_id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS clause_category_assignments (
                clause_id TEXT NOT NULL,
                category_code TEXT NOT NULL,
                confidence_score REAL NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                PRIMARY KEY (clause_id, category_code, created_at),
                FOREIGN KEY (clause_id) REFERENCES clauses(clause_id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS document_classifications (
                document_id TEXT NOT NULL,
                classification_code TEXT NOT NULL,
                confidence_score REAL NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                PRIMARY KEY (document_id, created_at),
                FOREIGN KEY (document_id) REFERENCES documents(document_id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS document_summaries (
                summary_id TEXT PRIMARY KEY,
                document_id TEXT NOT NULL,
                summary_text TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                FOREIGN KEY (document_id) REFERENCES documents(document_id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS document_embeddings (
                embedding_id TEXT PRIMARY KEY,
                document_id TEXT NOT NULL,
                embedding_vector TEXT NOT NULL,
                status TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                FOREIGN KEY (document_id) REFERENCES documents(document_id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS processing_events (
                event_id TEXT PRIMARY KEY,
                tenant_id TEXT NOT NULL,
                document_id TEXT,
                event_type TEXT NOT NULL,
                event_message TEXT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                FOREIGN KEY (tenant_id) REFERENCES tenants(tenant_id),
                FOREIGN KEY (document_id) REFERENCES documents(document_id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_documents_tenant_created ON documents(tenant_id, created_at DESC);
            CREATE UNIQUE INDEX IF NOT EXISTS idx_documents_tenant_idempotency ON documents(tenant_id, idempotency_key);
        ";
        command.ExecuteNonQuery();
    }

    private void SaveInternal(NpgsqlConnection connection, NpgsqlTransaction transaction, Document document)
    {
        var now = DateTimeOffset.UtcNow;
        var tenantId = document.TenantId.Value;
        var tenantCode = string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId;

        using (var tenantCommand = connection.CreateCommand())
        {
            tenantCommand.Transaction = transaction;
            tenantCommand.CommandText = "INSERT INTO tenants(tenant_id, tenant_code, created_at) VALUES(@tenantId, @tenantCode, @createdAt) ON CONFLICT (tenant_id) DO NOTHING";
            tenantCommand.Parameters.AddWithValue("tenantId", tenantId);
            tenantCommand.Parameters.AddWithValue("tenantCode", tenantCode);
            tenantCommand.Parameters.AddWithValue("createdAt", now.UtcDateTime);
            tenantCommand.ExecuteNonQuery();
        }

        using (var deleteAssignmentsCommand = connection.CreateCommand())
        {
            deleteAssignmentsCommand.Transaction = transaction;
            deleteAssignmentsCommand.CommandText = "DELETE FROM clause_category_assignments WHERE clause_id IN (SELECT clause_id FROM clauses WHERE document_id = @documentId)";
            deleteAssignmentsCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            deleteAssignmentsCommand.ExecuteNonQuery();
        }

        using (var deleteClausesCommand = connection.CreateCommand())
        {
            deleteClausesCommand.Transaction = transaction;
            deleteClausesCommand.CommandText = "DELETE FROM clauses WHERE document_id = @documentId";
            deleteClausesCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            deleteClausesCommand.ExecuteNonQuery();
        }

        using (var deleteRevisionsCommand = connection.CreateCommand())
        {
            deleteRevisionsCommand.Transaction = transaction;
            deleteRevisionsCommand.CommandText = "DELETE FROM document_revisions WHERE document_id = @documentId";
            deleteRevisionsCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            deleteRevisionsCommand.ExecuteNonQuery();
        }

        using (var deleteClassificationsCommand = connection.CreateCommand())
        {
            deleteClassificationsCommand.Transaction = transaction;
            deleteClassificationsCommand.CommandText = "DELETE FROM document_classifications WHERE document_id = @documentId";
            deleteClassificationsCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            deleteClassificationsCommand.ExecuteNonQuery();
        }

        using (var deleteSummariesCommand = connection.CreateCommand())
        {
            deleteSummariesCommand.Transaction = transaction;
            deleteSummariesCommand.CommandText = "DELETE FROM document_summaries WHERE document_id = @documentId";
            deleteSummariesCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            deleteSummariesCommand.ExecuteNonQuery();
        }

        using (var deleteEmbeddingsCommand = connection.CreateCommand())
        {
            deleteEmbeddingsCommand.Transaction = transaction;
            deleteEmbeddingsCommand.CommandText = "DELETE FROM document_embeddings WHERE document_id = @documentId";
            deleteEmbeddingsCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            deleteEmbeddingsCommand.ExecuteNonQuery();
        }

        using (var deleteEventsCommand = connection.CreateCommand())
        {
            deleteEventsCommand.Transaction = transaction;
            deleteEventsCommand.CommandText = "DELETE FROM processing_events WHERE document_id = @documentId";
            deleteEventsCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            deleteEventsCommand.ExecuteNonQuery();
        }

        using (var documentCommand = connection.CreateCommand())
        {
            documentCommand.Transaction = transaction;
            documentCommand.CommandText = @"
                INSERT INTO documents (
                    document_id,
                    tenant_id,
                    document_key,
                    source,
                    source_reference,
                    source_name,
                    document_type,
                    format,
                    ingestion_state,
                    current_outcome,
                    current_processing_stage,
                    correlation_id,
                    idempotency_key,
                    file_size_bytes,
                    mime_type,
                    language,
                    page_count,
                    author,
                    creation_date,
                    raw_text,
                    normalized_text,
                    rejection_reason,
                    created_at,
                    updated_at
                ) VALUES (
                    @documentId,
                    @tenantId,
                    @documentKey,
                    @source,
                    @sourceReference,
                    @sourceName,
                    @documentType,
                    @format,
                    @ingestionState,
                    @currentOutcome,
                    @currentProcessingStage,
                    @correlationId,
                    @idempotencyKey,
                    @fileSizeBytes,
                    @mimeType,
                    @language,
                    @pageCount,
                    @author,
                    @creationDate,
                    @rawText,
                    @normalizedText,
                    @rejectionReason,
                    @createdAt,
                    @updatedAt
                )
                ON CONFLICT (document_id) DO UPDATE SET
                    tenant_id = EXCLUDED.tenant_id,
                    document_key = EXCLUDED.document_key,
                    source = EXCLUDED.source,
                    source_reference = EXCLUDED.source_reference,
                    source_name = EXCLUDED.source_name,
                    document_type = EXCLUDED.document_type,
                    format = EXCLUDED.format,
                    ingestion_state = EXCLUDED.ingestion_state,
                    current_outcome = EXCLUDED.current_outcome,
                    current_processing_stage = EXCLUDED.current_processing_stage,
                    correlation_id = EXCLUDED.correlation_id,
                    idempotency_key = EXCLUDED.idempotency_key,
                    file_size_bytes = EXCLUDED.file_size_bytes,
                    mime_type = EXCLUDED.mime_type,
                    language = EXCLUDED.language,
                    page_count = EXCLUDED.page_count,
                    author = EXCLUDED.author,
                    creation_date = EXCLUDED.creation_date,
                    raw_text = EXCLUDED.raw_text,
                    normalized_text = EXCLUDED.normalized_text,
                    rejection_reason = EXCLUDED.rejection_reason,
                    updated_at = EXCLUDED.updated_at";
            documentCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            documentCommand.Parameters.AddWithValue("tenantId", tenantId);
            documentCommand.Parameters.AddWithValue("documentKey", document.Id.Value);
            documentCommand.Parameters.AddWithValue("source", document.Source.Value);
            documentCommand.Parameters.AddWithValue("sourceReference", document.Provenance.SourceReference);
            documentCommand.Parameters.AddWithValue("sourceName", (object?)document.Provenance.SourceName ?? DBNull.Value);
            documentCommand.Parameters.AddWithValue("documentType", document.DetectedDocumentType?.Value ?? "Unknown");
            documentCommand.Parameters.AddWithValue("format", document.Format.Value);
            documentCommand.Parameters.AddWithValue("ingestionState", document.State.ToString());
            documentCommand.Parameters.AddWithValue("currentOutcome", document.Outcome?.ToString() ?? string.Empty);
            documentCommand.Parameters.AddWithValue("currentProcessingStage", document.ProcessingStage.ToString());
            documentCommand.Parameters.AddWithValue("correlationId", (object?)document.CorrelationId.Value ?? DBNull.Value);
            documentCommand.Parameters.AddWithValue("idempotencyKey", document.IdempotencyKey.Value);
            documentCommand.Parameters.AddWithValue("fileSizeBytes", document.Metadata.FileSizeBytes);
            documentCommand.Parameters.AddWithValue("mimeType", document.Metadata.MimeType);
            documentCommand.Parameters.AddWithValue("language", (object?)document.Metadata.Language ?? DBNull.Value);
            documentCommand.Parameters.AddWithValue("pageCount", (object?)document.Metadata.PageCount ?? DBNull.Value);
            documentCommand.Parameters.AddWithValue("author", (object?)document.Metadata.Author ?? DBNull.Value);
            documentCommand.Parameters.AddWithValue("creationDate", (object?)document.Metadata.CreationDate?.UtcDateTime ?? DBNull.Value);
            documentCommand.Parameters.AddWithValue("rawText", (object?)document.ExtractedText?.Value ?? DBNull.Value);
            documentCommand.Parameters.AddWithValue("normalizedText", (object?)document.NormalizedText?.Value ?? DBNull.Value);
            documentCommand.Parameters.AddWithValue("rejectionReason", (object?)document.RejectionReason?.Value ?? DBNull.Value);
            documentCommand.Parameters.AddWithValue("createdAt", now.UtcDateTime);
            documentCommand.Parameters.AddWithValue("updatedAt", now.UtcDateTime);
            documentCommand.ExecuteNonQuery();
        }

        foreach (var revision in document.Revisions)
        {
            using var revisionCommand = connection.CreateCommand();
            revisionCommand.Transaction = transaction;
            revisionCommand.CommandText = @"
                INSERT INTO document_revisions (
                    revision_id,
                    document_id,
                    version,
                    revision_timestamp,
                    outcome,
                    processing_stage,
                    created_at
                ) VALUES (
                    @revisionId,
                    @documentId,
                    @version,
                    @revisionTimestamp,
                    @outcome,
                    @processingStage,
                    @createdAt
                )
                ON CONFLICT (document_id, version) DO NOTHING";
            revisionCommand.Parameters.AddWithValue("revisionId", $"{document.Id.Value}:{revision.Version}");
            revisionCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            revisionCommand.Parameters.AddWithValue("version", revision.Version);
            revisionCommand.Parameters.AddWithValue("revisionTimestamp", revision.Timestamp.UtcDateTime);
            revisionCommand.Parameters.AddWithValue("outcome", revision.Outcome.ToString());
            revisionCommand.Parameters.AddWithValue("processingStage", revision.ProcessingStage.ToString());
            revisionCommand.Parameters.AddWithValue("createdAt", now.UtcDateTime);
            revisionCommand.ExecuteNonQuery();
        }

        foreach (var clause in document.Clauses)
        {
            using var clauseCommand = connection.CreateCommand();
            clauseCommand.Transaction = transaction;
            clauseCommand.CommandText = @"
                INSERT INTO clauses (
                    clause_id,
                    document_id,
                    sequence,
                    number_label,
                    text,
                    span_start,
                    span_end,
                    created_at
                ) VALUES (
                    @clauseId,
                    @documentId,
                    @sequence,
                    @numberLabel,
                    @text,
                    @spanStart,
                    @spanEnd,
                    @createdAt
                )";
            clauseCommand.Parameters.AddWithValue("clauseId", clause.Id.Value);
            clauseCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            clauseCommand.Parameters.AddWithValue("sequence", clause.Sequence);
            clauseCommand.Parameters.AddWithValue("numberLabel", (object?)clause.NumberLabel?.Value ?? DBNull.Value);
            clauseCommand.Parameters.AddWithValue("text", clause.Text.Value);
            clauseCommand.Parameters.AddWithValue("spanStart", clause.Span.Start);
            clauseCommand.Parameters.AddWithValue("spanEnd", clause.Span.End);
            clauseCommand.Parameters.AddWithValue("createdAt", now.UtcDateTime);
            clauseCommand.ExecuteNonQuery();
        }

        foreach (var assignment in document.CategoryAssignments)
        {
            using var assignmentCommand = connection.CreateCommand();
            assignmentCommand.Transaction = transaction;
            assignmentCommand.CommandText = @"
                INSERT INTO clause_category_assignments (
                    clause_id,
                    category_code,
                    confidence_score,
                    created_at
                ) VALUES (
                    @clauseId,
                    @categoryCode,
                    @confidenceScore,
                    @createdAt
                )";
            assignmentCommand.Parameters.AddWithValue("clauseId", assignment.ClauseId.Value);
            assignmentCommand.Parameters.AddWithValue("categoryCode", assignment.CategoryCode.Value);
            assignmentCommand.Parameters.AddWithValue("confidenceScore", assignment.ConfidenceScore.Value);
            assignmentCommand.Parameters.AddWithValue("createdAt", now.UtcDateTime);
            assignmentCommand.ExecuteNonQuery();
        }

        if (document.DocumentClassification is not null)
        {
            using var classificationCommand = connection.CreateCommand();
            classificationCommand.Transaction = transaction;
            classificationCommand.CommandText = @"
                INSERT INTO document_classifications (
                    document_id,
                    classification_code,
                    confidence_score,
                    created_at
                ) VALUES (
                    @documentId,
                    @classificationCode,
                    @confidenceScore,
                    @createdAt
                )";
            classificationCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            classificationCommand.Parameters.AddWithValue("classificationCode", document.DocumentClassification.ClassificationCode.Value);
            classificationCommand.Parameters.AddWithValue("confidenceScore", document.DocumentClassification.ConfidenceScore.Value);
            classificationCommand.Parameters.AddWithValue("createdAt", now.UtcDateTime);
            classificationCommand.ExecuteNonQuery();
        }

        if (document.DocumentSummary is not null)
        {
            using var summaryCommand = connection.CreateCommand();
            summaryCommand.Transaction = transaction;
            summaryCommand.CommandText = @"
                INSERT INTO document_summaries (
                    summary_id,
                    document_id,
                    summary_text,
                    created_at
                ) VALUES (
                    @summaryId,
                    @documentId,
                    @summaryText,
                    @createdAt
                )";
            summaryCommand.Parameters.AddWithValue("summaryId", $"{document.Id.Value}:summary");
            summaryCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            summaryCommand.Parameters.AddWithValue("summaryText", document.DocumentSummary.SummaryText.Value);
            summaryCommand.Parameters.AddWithValue("createdAt", now.UtcDateTime);
            summaryCommand.ExecuteNonQuery();
        }

        if (document.DocumentEmbedding is not null)
        {
            using var embeddingCommand = connection.CreateCommand();
            embeddingCommand.Transaction = transaction;
            embeddingCommand.CommandText = @"
                INSERT INTO document_embeddings (
                    embedding_id,
                    document_id,
                    embedding_vector,
                    status,
                    created_at
                ) VALUES (
                    @embeddingId,
                    @documentId,
                    @embeddingVector,
                    @status,
                    @createdAt
                )";
            embeddingCommand.Parameters.AddWithValue("embeddingId", $"{document.Id.Value}:embedding");
            embeddingCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            embeddingCommand.Parameters.AddWithValue("embeddingVector", string.Join(",", document.DocumentEmbedding.EmbeddingVector.Values));
            embeddingCommand.Parameters.AddWithValue("status", document.DocumentEmbedding.Status.ToString());
            embeddingCommand.Parameters.AddWithValue("createdAt", now.UtcDateTime);
            embeddingCommand.ExecuteNonQuery();
        }

        using (var eventCommand = connection.CreateCommand())
        {
            eventCommand.Transaction = transaction;
            eventCommand.CommandText = @"
                INSERT INTO processing_events (
                    event_id,
                    tenant_id,
                    document_id,
                    event_type,
                    event_message,
                    created_at
                ) VALUES (
                    @eventId,
                    @tenantId,
                    @documentId,
                    @eventType,
                    @eventMessage,
                    @createdAt
                )";
            eventCommand.Parameters.AddWithValue("eventId", $"{document.Id.Value}:persisted");
            eventCommand.Parameters.AddWithValue("tenantId", document.TenantId.Value);
            eventCommand.Parameters.AddWithValue("documentId", document.Id.Value);
            eventCommand.Parameters.AddWithValue("eventType", "DocumentPersisted");
            eventCommand.Parameters.AddWithValue("eventMessage", "Document state persisted to the repository.");
            eventCommand.Parameters.AddWithValue("createdAt", now.UtcDateTime);
            eventCommand.ExecuteNonQuery();
        }
    }

    private Document? GetByTenantAndIdempotencyKeyInternal(NpgsqlConnection connection, NpgsqlTransaction? transaction, string tenantId, string idempotencyKey)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT document_id FROM documents WHERE tenant_id = @tenantId AND idempotency_key = @idempotencyKey";
        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.AddWithValue("idempotencyKey", idempotencyKey);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var documentId = reader.GetString(0);
        return GetById(documentId);
    }

    private Document MapDocument(NpgsqlDataReader reader, NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        var documentId = reader.GetString(0);
        var tenantId = reader.GetString(1);
        var documentKey = reader.GetString(2);
        var source = reader.GetString(3);
        var sourceReference = reader.GetString(4);
        var sourceName = reader.IsDBNull(5) ? null : reader.GetString(5);
        var documentType = reader.GetString(6);
        var format = reader.GetString(7);
        var ingestionState = reader.GetString(8);
        var currentOutcome = reader.GetString(9);
        var currentProcessingStage = reader.GetString(10);
        var correlationId = reader.IsDBNull(11) ? null : reader.GetString(11);
        var idempotencyKey = reader.IsDBNull(12) ? null : reader.GetString(12);
        var fileSizeBytes = reader.GetInt64(13);
        var mimeType = reader.GetString(14);
        var language = reader.IsDBNull(15) ? null : reader.GetString(15);
        int? pageCount = reader.IsDBNull(16) ? null : reader.GetInt32(16);
        var author = reader.IsDBNull(17) ? null : reader.GetString(17);
        DateTimeOffset? creationDate = reader.IsDBNull(18) ? null : reader.GetFieldValue<DateTimeOffset>(18);
        var rawText = reader.IsDBNull(19) ? null : reader.GetString(19);
        var normalizedText = reader.IsDBNull(20) ? null : reader.GetString(20);
        var rejectionReason = reader.IsDBNull(21) ? null : reader.GetString(21);

        var metadata = new DocumentMetadata(fileSizeBytes, mimeType, language, pageCount, author, creationDate);
        var provenance = new Provenance(sourceReference, sourceName);
        var revisionList = LoadRevisions(connection, transaction, documentId);
        var clauses = LoadClauses(connection, transaction, documentId);
        var categoryAssignments = LoadCategoryAssignments(connection, transaction, documentId);
        var documentClassification = LoadDocumentClassification(connection, transaction, documentId);
        var documentSummary = LoadDocumentSummary(connection, transaction, documentId);
        var documentEmbedding = LoadDocumentEmbedding(connection, transaction, documentId);

        var document = Document.Rehydrate(
            new DocumentId(documentId),
            new TenantId(tenantId),
            new DocumentSource(source),
            new DocumentFormat(format),
            metadata,
            provenance,
            new CorrelationId(correlationId ?? string.Empty),
            new IdempotencyKey(idempotencyKey ?? string.Empty),
            ParseState(ingestionState),
            ParseProcessingStage(currentProcessingStage),
            ParseOutcome(currentOutcome),
            string.IsNullOrWhiteSpace(rejectionReason) ? null : new RejectionReason(rejectionReason),
            string.IsNullOrWhiteSpace(documentType) || string.Equals(documentType, "Unknown", StringComparison.OrdinalIgnoreCase) ? null : new DocumentType(documentType),
            string.IsNullOrWhiteSpace(rawText) ? null : new RawText(rawText),
            string.IsNullOrWhiteSpace(normalizedText) ? null : new NormalizedText(normalizedText),
            revisionList,
            clauses,
            categoryAssignments,
            documentClassification,
            documentSummary,
            documentEmbedding);

        return document;
    }

    private IReadOnlyList<DocumentRevision> LoadRevisions(NpgsqlConnection connection, NpgsqlTransaction? transaction, string documentId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT version, revision_timestamp, outcome, processing_stage FROM document_revisions WHERE document_id = @documentId ORDER BY version ASC";
        command.Parameters.AddWithValue("documentId", documentId);

        using var reader = command.ExecuteReader();
        var revisions = new List<DocumentRevision>();
        while (reader.Read())
        {
            revisions.Add(new DocumentRevision(
                reader.GetInt32(0),
                reader.GetFieldValue<DateTimeOffset>(1),
                ParseOutcomeRequired(reader.GetString(2)),
                ParseProcessingStage(reader.GetString(3))));
        }

        return revisions;
    }

    private IReadOnlyList<Clause> LoadClauses(NpgsqlConnection connection, NpgsqlTransaction? transaction, string documentId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT clause_id, sequence, number_label, text, span_start, span_end FROM clauses WHERE document_id = @documentId ORDER BY sequence ASC";
        command.Parameters.AddWithValue("documentId", documentId);

        using var reader = command.ExecuteReader();
        var clauses = new List<Clause>();
        while (reader.Read())
        {
            clauses.Add(Clause.Create(
                new ClauseId(reader.GetString(0)),
                reader.GetInt32(1),
                new ClauseText(reader.GetString(3)),
                new ClauseSpan(reader.GetInt32(4), reader.GetInt32(5)),
                reader.IsDBNull(2) ? null : new ClauseNumberLabel(reader.GetString(2))));
        }

        return clauses;
    }

    private IReadOnlyList<ClauseCategoryAssignment> LoadCategoryAssignments(NpgsqlConnection connection, NpgsqlTransaction? transaction, string documentId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            SELECT c.clause_id, c.category_code, c.confidence_score
            FROM clause_category_assignments c
            INNER JOIN clauses cl ON c.clause_id = cl.clause_id
            WHERE cl.document_id = @documentId
            ORDER BY c.clause_id ASC";
        command.Parameters.AddWithValue("documentId", documentId);

        using var reader = command.ExecuteReader();
        var assignments = new List<ClauseCategoryAssignment>();
        while (reader.Read())
        {
            assignments.Add(ClauseCategoryAssignment.Create(
                new ClauseId(reader.GetString(0)),
                new ClauseCategoryCode(reader.GetString(1)),
                new ConfidenceScore(reader.GetDecimal(2))));
        }

        return assignments;
    }

    private DocumentClassificationResult? LoadDocumentClassification(NpgsqlConnection connection, NpgsqlTransaction? transaction, string documentId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT classification_code, confidence_score FROM document_classifications WHERE document_id = @documentId ORDER BY created_at ASC LIMIT 1";
        command.Parameters.AddWithValue("documentId", documentId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return DocumentClassificationResult.Create(
            new DocumentClassificationCode(reader.GetString(0)),
            new ConfidenceScore(reader.GetDecimal(1)));
    }

    private DocumentSummaryResult? LoadDocumentSummary(NpgsqlConnection connection, NpgsqlTransaction? transaction, string documentId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT summary_text FROM document_summaries WHERE document_id = @documentId ORDER BY created_at ASC LIMIT 1";
        command.Parameters.AddWithValue("documentId", documentId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return DocumentSummaryResult.Create(new SummaryText(reader.GetString(0)));
    }

    private DocumentEmbeddingResult? LoadDocumentEmbedding(NpgsqlConnection connection, NpgsqlTransaction? transaction, string documentId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT embedding_vector FROM document_embeddings WHERE document_id = @documentId ORDER BY created_at ASC LIMIT 1";
        command.Parameters.AddWithValue("documentId", documentId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var values = reader.GetString(0)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(decimal.Parse)
            .ToList();

        return DocumentEmbeddingResult.Create(new EmbeddingVector(values));
    }

    private static DocumentPersistenceContract ToPersistenceContract(Document document)
    {
        return new DocumentPersistenceContract(
            document.Id.Value,
            document.TenantId.Value,
            document.Source.Value,
            document.Format.Value,
            document.Metadata.FileSizeBytes,
            document.Metadata.MimeType,
            document.Metadata.Language,
            document.Metadata.PageCount,
            document.Metadata.Author,
            document.Metadata.CreationDate,
            document.Provenance.SourceReference,
            document.Provenance.SourceName,
            document.CorrelationId.Value,
            document.IdempotencyKey.Value,
            document.Outcome?.ToString(),
            document.RejectionReason?.Value,
            document.ProcessingStage.ToString(),
            document.State.ToString(),
            document.DetectedDocumentType?.Value,
            document.ExtractedText?.Value,
            document.NormalizedText?.Value,
            document.Revisions.Select(revision => new DocumentRevisionPersistenceContract(
                revision.Version,
                revision.Timestamp,
                revision.Outcome.ToString(),
                revision.ProcessingStage.ToString())).ToList(),
            document.Clauses.Select(clause => new ClausePersistenceContract(
                clause.Id.Value,
                clause.Sequence,
                clause.NumberLabel?.Value,
                clause.Text.Value,
                clause.Span.Start,
                clause.Span.End)).ToList(),
            document.CategoryAssignments.Select(assignment => new ClauseCategoryAssignmentPersistenceContract(
                assignment.ClauseId.Value,
                assignment.CategoryCode.Value,
                assignment.ConfidenceScore.Value)).ToList(),
            document.DocumentClassification is null ? null : new List<DocumentClassificationPersistenceContract>
            {
                new(document.DocumentClassification.ClassificationCode.Value, document.DocumentClassification.ConfidenceScore.Value)
            },
            document.DocumentSummary is null ? null : new List<DocumentSummaryPersistenceContract>
            {
                new(document.DocumentSummary.SummaryText.Value)
            },
            document.DocumentEmbedding is null ? null : new List<DocumentEmbeddingPersistenceContract>
            {
                new(document.DocumentEmbedding.EmbeddingVector.Values.ToList())
            });
    }

    private NpgsqlConnection OpenConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static IngestionState ParseState(string state)
    {
        return Enum.TryParse<IngestionState>(state, true, out var parsed)
            ? parsed
            : IngestionState.PendingAcceptance;
    }

    private static ProcessingStage ParseProcessingStage(string processingStage)
    {
        return Enum.TryParse<ProcessingStage>(processingStage, true, out var parsed)
            ? parsed
            : ProcessingStage.None;
    }

    private static IngestionOutcome? ParseOutcome(string? outcome)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            return null;
        }

        return Enum.TryParse<IngestionOutcome>(outcome, true, out var parsed)
            ? parsed
            : IngestionOutcome.Accepted;
    }

    private static IngestionOutcome ParseOutcomeRequired(string outcome)
    {
        return Enum.TryParse<IngestionOutcome>(outcome, true, out var parsed)
            ? parsed
            : IngestionOutcome.Accepted;
    }
}
