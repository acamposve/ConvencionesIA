using DocumentIngestion.Domain;
using System.Text.Json;

namespace DocumentIngestion.Application;

public sealed class FileSystemDocumentRepository : IDocumentRepository
{
    private const string IdempotencyIndexFileName = "idempotency-index.json";
    private readonly string _storageDirectory;
    private readonly string _idempotencyIndexPath;
    private readonly object _indexGate = new();
    private readonly Dictionary<string, string> _idempotencyIndex = new(StringComparer.OrdinalIgnoreCase);
    private bool _indexLoaded;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public FileSystemDocumentRepository(string storageDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        _storageDirectory = storageDirectory;
        _idempotencyIndexPath = Path.Combine(_storageDirectory, IdempotencyIndexFileName);
        Directory.CreateDirectory(_storageDirectory);
    }

    public void Save(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var path = GetPath(document.Id.Value);
        var contract = ToContract(document);
        var payload = JsonSerializer.Serialize(contract, JsonOptions);
        File.WriteAllText(path, payload);
        UpsertIdempotencyIndex(document.TenantId.Value, document.IdempotencyKey.Value, document.Id.Value);
    }

    public bool TryCreate(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var lockPath = GetIdempotencyLockPath(document.TenantId.Value, document.IdempotencyKey.Value);
        using var lockStream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        if (GetByTenantAndIdempotencyKey(document.TenantId.Value, document.IdempotencyKey.Value) is not null)
        {
            return false;
        }

        Save(document);
        return true;
    }

    public Document? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var path = GetPath(id);
        if (!File.Exists(path))
        {
            return null;
        }

        var payload = File.ReadAllText(path);
        return DeserializeDocument(payload);
    }

    public Document? GetByTenantAndIdempotencyKey(string tenantId, string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        var compositeKey = BuildCompositeKey(tenantId, idempotencyKey);
        var indexedDocumentId = TryGetIndexedDocumentId(compositeKey);
        if (!string.IsNullOrWhiteSpace(indexedDocumentId))
        {
            var indexedDocument = GetById(indexedDocumentId);
            if (indexedDocument is not null
                && string.Equals(indexedDocument.TenantId.Value, tenantId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(indexedDocument.IdempotencyKey.Value, idempotencyKey, StringComparison.OrdinalIgnoreCase))
            {
                return indexedDocument;
            }
        }

        foreach (var file in Directory.GetFiles(_storageDirectory, "*.json"))
        {
            if (IsIndexFile(file))
            {
                continue;
            }

            var payload = File.ReadAllText(file);
            var document = DeserializeDocument(payload);
            if (document is not null
                && string.Equals(document.TenantId.Value, tenantId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(document.IdempotencyKey.Value, idempotencyKey, StringComparison.OrdinalIgnoreCase))
            {
                UpsertIdempotencyIndex(tenantId, idempotencyKey, document.Id.Value);
                return document;
            }
        }

        return null;
    }

    private static DocumentPersistenceContract ToContract(Document document)
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
            document.Revisions
                .Select(revision => new DocumentRevisionPersistenceContract(
                    revision.Version,
                    revision.Timestamp,
                    revision.Outcome.ToString(),
                    revision.ProcessingStage.ToString()))
                .ToList(),
            document.Clauses
                .Select(clause => new ClausePersistenceContract(
                    clause.Id.Value,
                    clause.Sequence,
                    clause.NumberLabel?.Value,
                    clause.Text.Value,
                    clause.Span.Start,
                    clause.Span.End))
                .ToList(),
            document.CategoryAssignments
                .Select(assignment => new ClauseCategoryAssignmentPersistenceContract(
                    assignment.ClauseId.Value,
                    assignment.CategoryCode.Value,
                    assignment.ConfidenceScore.Value))
                .ToList(),
            document.DocumentClassification is null
                ? null
                : new List<DocumentClassificationPersistenceContract>
                {
                    new(document.DocumentClassification.ClassificationCode.Value, document.DocumentClassification.ConfidenceScore.Value)
                },
            document.DocumentSummary is null
                ? null
                : new List<DocumentSummaryPersistenceContract>
                {
                    new(document.DocumentSummary.SummaryText.Value)
                },
            document.DocumentEmbedding is null
                ? null
                : new List<DocumentEmbeddingPersistenceContract>
                {
                    new(document.DocumentEmbedding.EmbeddingVector.Values.ToList())
                });
    }

    private static Document? DeserializeDocument(string payload)
    {
        var contract = JsonSerializer.Deserialize<DocumentPersistenceContract>(payload);
        if (contract is not null)
        {
            return FromContract(contract);
        }

        var legacyContract = JsonSerializer.Deserialize<LegacyDocumentPersistenceContract>(payload);
        return legacyContract is null ? null : FromLegacyContract(legacyContract);
    }

    private static Document FromContract(DocumentPersistenceContract contract)
    {
        var metadata = new DocumentMetadata(
            contract.FileSizeBytes,
            contract.MimeType,
            contract.Language,
            contract.PageCount,
            contract.Author,
            contract.CreationDate);

        var provenance = new Provenance(contract.SourceReference, contract.SourceName);
        var tenantId = new TenantId(contract.TenantId);
        var source = new DocumentSource(contract.Source);
        var format = new DocumentFormat(contract.Format);
        var correlationId = new CorrelationId(contract.CorrelationId);
        var idempotencyKey = new IdempotencyKey(contract.IdempotencyKey);
        var documentId = new DocumentId(contract.Id);

        var state = ParseState(contract.State);
        var processingStage = ParseProcessingStage(contract.ProcessingStage);
        var outcome = ParseOutcome(contract.Outcome);
        var rejectionReason = string.IsNullOrWhiteSpace(contract.RejectionReason) ? null : new RejectionReason(contract.RejectionReason);
        var detectedDocumentType = string.IsNullOrWhiteSpace(contract.DetectedDocumentType) ? null : new DocumentType(contract.DetectedDocumentType);
        var extractedText = string.IsNullOrWhiteSpace(contract.ExtractedText) ? null : new RawText(contract.ExtractedText);
        var normalizedText = string.IsNullOrWhiteSpace(contract.NormalizedText) ? null : new NormalizedText(contract.NormalizedText);

        var revisions = contract.Revisions
            .Select(revision => new DocumentRevision(
                revision.Version,
                revision.Timestamp,
                ParseOutcomeRequired(revision.Outcome),
                ParseProcessingStage(revision.ProcessingStage)))
            .ToList();

        var clauses = (contract.Clauses ?? [])
            .Select(clause => Clause.Create(
                new ClauseId(clause.Id),
                clause.Sequence,
                new ClauseText(clause.Text),
                new ClauseSpan(clause.SpanStart, clause.SpanEnd),
                string.IsNullOrWhiteSpace(clause.NumberLabel) ? null : new ClauseNumberLabel(clause.NumberLabel)))
            .ToList();

        var categoryAssignments = (contract.CategoryAssignments ?? [])
            .Select(assignment => ClauseCategoryAssignment.Create(
                new ClauseId(assignment.ClauseId),
                new ClauseCategoryCode(assignment.CategoryCode),
                new ConfidenceScore(assignment.ConfidenceScore)))
            .ToList();

        DocumentClassificationResult? documentClassification = null;
        if (contract.DocumentClassifications is { Count: > 0 })
        {
            var classification = contract.DocumentClassifications[0];
            documentClassification = DocumentClassificationResult.Create(
                new DocumentClassificationCode(classification.ClassificationCode),
                new ConfidenceScore(classification.ConfidenceScore));
        }

        DocumentSummaryResult? documentSummary = null;
        if (contract.DocumentSummaries is { Count: > 0 })
        {
            var summary = contract.DocumentSummaries[0];
            documentSummary = DocumentSummaryResult.Create(
                new SummaryText(summary.SummaryText));
        }

        DocumentEmbeddingResult? documentEmbedding = null;
        if (contract.DocumentEmbeddings is { Count: > 0 })
        {
            var embedding = contract.DocumentEmbeddings[0];
            documentEmbedding = DocumentEmbeddingResult.Create(
                new EmbeddingVector(embedding.EmbeddingValues.ToList()));
        }

        var document = Document.Rehydrate(
            documentId,
            tenantId,
            source,
            format,
            metadata,
            provenance,
            correlationId,
            idempotencyKey,
            state,
            processingStage,
            outcome,
            rejectionReason,
            detectedDocumentType,
            extractedText,
            normalizedText,
            revisions,
            clauses,
            categoryAssignments,
            documentClassification,
            documentSummary,
            documentEmbedding);

        return document;
    }

    private static Document FromLegacyContract(LegacyDocumentPersistenceContract legacyContract)
    {
        var contract = new DocumentPersistenceContract(
            legacyContract.Id,
            legacyContract.TenantId,
            legacyContract.Source,
            legacyContract.Format,
            legacyContract.FileSizeBytes,
            legacyContract.MimeType,
            legacyContract.Language,
            legacyContract.PageCount,
            legacyContract.Author,
            legacyContract.CreationDate,
            legacyContract.SourceReference,
            legacyContract.SourceName,
            legacyContract.CorrelationId,
            legacyContract.IdempotencyKey,
            string.IsNullOrWhiteSpace(legacyContract.Outcome) ? null : legacyContract.Outcome,
            legacyContract.RejectionReason,
            legacyContract.ProcessingStage,
            legacyContract.State,
            null,
            legacyContract.ExtractedText,
            legacyContract.NormalizedText,
            BuildLegacyRevisions(legacyContract));

        return FromContract(contract);
    }

    private static IReadOnlyList<DocumentRevisionPersistenceContract> BuildLegacyRevisions(LegacyDocumentPersistenceContract legacyContract)
    {
        var state = ParseState(legacyContract.State);
        if (state == IngestionState.PendingAcceptance)
        {
            return [];
        }

        var outcome = ParseOutcomeRequired(string.IsNullOrWhiteSpace(legacyContract.Outcome) ? state.ToString() : legacyContract.Outcome);
        var processingStage = ParseProcessingStage(legacyContract.ProcessingStage);
        return [new DocumentRevisionPersistenceContract(1, DateTimeOffset.UtcNow, outcome.ToString(), processingStage.ToString())];
    }

    private static IngestionState ParseState(string state)
    {
        if (!Enum.TryParse<IngestionState>(state, true, out var parsedState))
        {
            throw new InvalidOperationException($"Unknown document state '{state}'.");
        }

        return parsedState;
    }

    private static ProcessingStage ParseProcessingStage(string processingStage)
    {
        if (!Enum.TryParse<ProcessingStage>(processingStage, true, out var parsedStage))
        {
            throw new InvalidOperationException($"Unknown processing stage '{processingStage}'.");
        }

        return parsedStage;
    }

    private static IngestionOutcome? ParseOutcome(string? outcome)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            return null;
        }

        return ParseOutcomeRequired(outcome);
    }

    private static IngestionOutcome ParseOutcomeRequired(string outcome)
    {
        if (!Enum.TryParse<IngestionOutcome>(outcome, true, out var parsedOutcome))
        {
            throw new InvalidOperationException($"Unknown processing outcome '{outcome}'.");
        }

        return parsedOutcome;
    }

    private string GetPath(string id)
    {
        return Path.Combine(_storageDirectory, $"{id}.json");
    }

    private string GetIdempotencyLockPath(string tenantId, string idempotencyKey)
    {
        var compositeKey = BuildCompositeKey(tenantId, idempotencyKey);
        return Path.Combine(_storageDirectory, $"{Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(compositeKey))}.lock");
    }

    private void UpsertIdempotencyIndex(string tenantId, string idempotencyKey, string documentId)
    {
        var compositeKey = BuildCompositeKey(tenantId, idempotencyKey);

        lock (_indexGate)
        {
            EnsureIndexLoadedUnsafe();
            _idempotencyIndex[compositeKey] = documentId;
            PersistIndexUnsafe();
        }
    }

    private string? TryGetIndexedDocumentId(string compositeKey)
    {
        lock (_indexGate)
        {
            EnsureIndexLoadedUnsafe();
            return _idempotencyIndex.TryGetValue(compositeKey, out var documentId) ? documentId : null;
        }
    }

    private void EnsureIndexLoadedUnsafe()
    {
        if (_indexLoaded)
        {
            return;
        }

        _idempotencyIndex.Clear();
        if (File.Exists(_idempotencyIndexPath))
        {
            var indexPayload = File.ReadAllText(_idempotencyIndexPath);
            var indexContract = JsonSerializer.Deserialize<IdempotencyIndexContract>(indexPayload);
            if (indexContract?.Entries is not null)
            {
                foreach (var entry in indexContract.Entries)
                {
                    _idempotencyIndex[entry.CompositeKey] = entry.DocumentId;
                }
            }
        }
        else
        {
            RebuildIndexUnsafe();
            PersistIndexUnsafe();
        }

        _indexLoaded = true;
    }

    private void RebuildIndexUnsafe()
    {
        foreach (var file in Directory.GetFiles(_storageDirectory, "*.json"))
        {
            if (IsIndexFile(file))
            {
                continue;
            }

            var payload = File.ReadAllText(file);
            var document = DeserializeDocument(payload);
            if (document is null)
            {
                continue;
            }

            var compositeKey = BuildCompositeKey(document.TenantId.Value, document.IdempotencyKey.Value);
            _idempotencyIndex[compositeKey] = document.Id.Value;
        }
    }

    private void PersistIndexUnsafe()
    {
        var entries = _idempotencyIndex
            .Select(entry => new IdempotencyIndexEntryContract(entry.Key, entry.Value))
            .ToList();
        var payload = JsonSerializer.Serialize(new IdempotencyIndexContract(entries), JsonOptions);
        File.WriteAllText(_idempotencyIndexPath, payload);
    }

    private static string BuildCompositeKey(string tenantId, string idempotencyKey)
    {
        return $"{tenantId}:{idempotencyKey}";
    }

    private static bool IsIndexFile(string path)
    {
        return string.Equals(Path.GetFileName(path), IdempotencyIndexFileName, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record LegacyDocumentPersistenceContract(
        string Id,
        string TenantId,
        string Source,
        string Format,
        long FileSizeBytes,
        string MimeType,
        string? Language,
        int? PageCount,
        string? Author,
        DateTimeOffset? CreationDate,
        string SourceReference,
        string? SourceName,
        string CorrelationId,
        string IdempotencyKey,
        string Outcome,
        string? RejectionReason,
        string ProcessingStage,
        string State,
        string? ExtractedText,
        string? NormalizedText);

    private sealed record IdempotencyIndexContract(IReadOnlyList<IdempotencyIndexEntryContract> Entries);

    private sealed record IdempotencyIndexEntryContract(string CompositeKey, string DocumentId);
}
