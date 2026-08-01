using System.Collections.Generic;

namespace DocumentIngestion.Domain;

public sealed class Document
{
    private readonly List<DocumentRevision> _revisions = [];
    private readonly List<Clause> _clauses = [];
    private readonly List<ClauseCategoryAssignment> _categoryAssignments = [];
    private DocumentClassificationResult? _documentClassification;
    private DocumentSummaryResult? _documentSummary;
    private DocumentEmbeddingResult? _documentEmbedding;

    public Document(
        DocumentId id,
        TenantId tenantId,
        DocumentSource source,
        DocumentFormat format,
        DocumentMetadata metadata,
        Provenance provenance,
        CorrelationId correlationId,
        IdempotencyKey idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(correlationId);
        ArgumentNullException.ThrowIfNull(idempotencyKey);

        Id = id;
        TenantId = tenantId;
        Source = source;
        Format = format;
        Metadata = metadata;
        Provenance = provenance;
        CorrelationId = correlationId;
        IdempotencyKey = idempotencyKey;
        State = IngestionState.PendingAcceptance;
    }

    public DocumentId Id { get; }
    public TenantId TenantId { get; }
    public DocumentSource Source { get; }
    public DocumentFormat Format { get; }
    public DocumentMetadata Metadata { get; }
    public Provenance Provenance { get; }
    public CorrelationId CorrelationId { get; }
    public IdempotencyKey IdempotencyKey { get; }
    public IngestionState State { get; private set; }
    public ProcessingStage ProcessingStage { get; private set; }
    public IngestionOutcome? Outcome { get; private set; }
    public RejectionReason? RejectionReason { get; private set; }
    public DocumentType? DetectedDocumentType { get; private set; }
    public RawText? ExtractedText { get; private set; }
    public NormalizedText? NormalizedText { get; private set; }
    public IReadOnlyList<Clause> Clauses => _clauses.AsReadOnly();
    public IReadOnlyList<ClauseCategoryAssignment> CategoryAssignments => _categoryAssignments.AsReadOnly();
    public DocumentClassificationResult? DocumentClassification => _documentClassification;
    public DocumentSummaryResult? DocumentSummary => _documentSummary;
    public DocumentEmbeddingResult? DocumentEmbedding => _documentEmbedding;
    public bool HasDetectedDocumentType => DetectedDocumentType is not null;
    public bool HasExtractedText => ExtractedText is not null;
    public bool HasNormalizedText => NormalizedText is not null;
    public bool HasClauses => _clauses.Count > 0;
    public bool HasCategoryAssignments => _categoryAssignments.Count > 0;
    public bool HasDocumentClassification => _documentClassification is not null;
    public bool HasDocumentSummary => _documentSummary is not null;
    public bool HasDocumentEmbedding => _documentEmbedding is not null;
    public IReadOnlyList<DocumentRevision> Revisions => _revisions.AsReadOnly();

    public static Document Accept(
        DocumentId id,
        TenantId tenantId,
        DocumentSource source,
        DocumentFormat format,
        DocumentMetadata metadata,
        Provenance provenance,
        CorrelationId correlationId,
        IdempotencyKey idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(correlationId);
        ArgumentNullException.ThrowIfNull(idempotencyKey);

        ValidateAcceptanceInputs(tenantId, source, format, metadata, provenance, idempotencyKey);

        var document = new Document(id, tenantId, source, format, metadata, provenance, correlationId, idempotencyKey);
        document.AcceptInternal();
        return document;
    }

    public static Document Reject(
        DocumentId id,
        TenantId tenantId,
        DocumentSource source,
        DocumentFormat format,
        DocumentMetadata metadata,
        Provenance provenance,
        CorrelationId correlationId,
        IdempotencyKey idempotencyKey,
        RejectionReason rejectionReason)
    {
        ArgumentNullException.ThrowIfNull(rejectionReason);

        ValidateTenantContext(tenantId);

        var document = new Document(id, tenantId, source, format, metadata, provenance, correlationId, idempotencyKey);
        document.RejectInternal(rejectionReason);
        return document;
    }

    public static Document Rehydrate(
        DocumentId id,
        TenantId tenantId,
        DocumentSource source,
        DocumentFormat format,
        DocumentMetadata metadata,
        Provenance provenance,
        CorrelationId correlationId,
        IdempotencyKey idempotencyKey,
        IngestionState state,
        ProcessingStage processingStage,
        IngestionOutcome? outcome,
        RejectionReason? rejectionReason,
        DocumentType? detectedDocumentType,
        RawText? extractedText,
        NormalizedText? normalizedText,
        IReadOnlyList<DocumentRevision> revisions,
        IReadOnlyList<Clause>? clauses = null,
        IReadOnlyList<ClauseCategoryAssignment>? categoryAssignments = null,
        DocumentClassificationResult? documentClassification = null,
        DocumentSummaryResult? documentSummary = null,
        DocumentEmbeddingResult? documentEmbedding = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(correlationId);
        ArgumentNullException.ThrowIfNull(idempotencyKey);
        ArgumentNullException.ThrowIfNull(revisions);

        ValidateTenantContext(tenantId);
        ValidateRehydratedState(state, processingStage, outcome, rejectionReason, detectedDocumentType, extractedText, normalizedText);
        ValidateRevisions(revisions, state, outcome, processingStage);

        var document = new Document(id, tenantId, source, format, metadata, provenance, correlationId, idempotencyKey)
        {
            State = state,
            ProcessingStage = processingStage,
            Outcome = outcome,
            RejectionReason = rejectionReason,
            DetectedDocumentType = detectedDocumentType,
            ExtractedText = extractedText,
            NormalizedText = normalizedText
        };

        document._revisions.AddRange(revisions);
        if (clauses is not null)
        {
            document._clauses.AddRange(clauses);
        }

        if (categoryAssignments is not null)
        {
            document._categoryAssignments.AddRange(categoryAssignments);
        }

        if (documentClassification is not null)
        {
            document.RestoreDocumentClassification(documentClassification);
        }

        if (documentSummary is not null)
        {
            document.RestoreDocumentSummary(documentSummary);
        }

        if (documentEmbedding is not null)
        {
            document.RestoreDocumentEmbedding(documentEmbedding);
        }

        return document;
    }

    private void AcceptInternal()
    {
        if (State != IngestionState.PendingAcceptance)
        {
            throw new InvalidOperationException("Only pending acceptance documents can be accepted.");
        }

        State = IngestionState.Accepted;
        ProcessingStage = ProcessingStage.PendingProcessing;
        Outcome = IngestionOutcome.Accepted;
        RejectionReason = null;
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Accepted, ProcessingStage));
    }

    internal static void ValidateAcceptanceInputs(
        TenantId tenantId,
        DocumentSource source,
        DocumentFormat format,
        DocumentMetadata metadata,
        Provenance provenance,
        IdempotencyKey idempotencyKey)
    {
        ValidateTenantContext(tenantId);

        if (!IsSupportedSource(source))
        {
            throw new DomainValidationException("Unsupported source");
        }

        if (!IsSupportedFormat(format))
        {
            throw new DomainValidationException("Unsupported format");
        }

        if (!metadata.HasRequiredMetadata || string.IsNullOrWhiteSpace(provenance.SourceReference) || string.IsNullOrWhiteSpace(idempotencyKey.Value))
        {
            throw new DomainValidationException("Validation failure");
        }
    }

    internal static void ValidateTenantContext(TenantId tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId.Value))
        {
            throw new DomainValidationException("Invalid tenant context");
        }
    }

    private static bool IsSupportedSource(DocumentSource source)
    {
        return source.Value.Equals("Upload", StringComparison.OrdinalIgnoreCase)
            || source.Value.Equals("URL", StringComparison.OrdinalIgnoreCase)
            || source.Value.Equals("Cloud Storage", StringComparison.OrdinalIgnoreCase)
            || source.Value.Equals("External Integration", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedFormat(DocumentFormat format)
    {
        return format.Value.Equals("PDF", StringComparison.OrdinalIgnoreCase)
            || format.Value.Equals("Word", StringComparison.OrdinalIgnoreCase)
            || format.Value.Equals("Image", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateRehydratedState(
        IngestionState state,
        ProcessingStage processingStage,
        IngestionOutcome? outcome,
        RejectionReason? rejectionReason,
        DocumentType? detectedDocumentType,
        RawText? extractedText,
        NormalizedText? normalizedText)
    {
        if (normalizedText is not null && extractedText is null)
        {
            throw new DomainValidationException("Cannot restore normalized text without extracted text.");
        }

        if (detectedDocumentType is not null && detectedDocumentType.Value.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Unsupported document type");
        }

        switch (state)
        {
            case IngestionState.PendingAcceptance:
                if (processingStage != ProcessingStage.None || outcome is not null || rejectionReason is not null)
                {
                    throw new DomainValidationException("Invalid pending document state during rehydration.");
                }

                break;
            case IngestionState.Accepted:
                if ((processingStage != ProcessingStage.PendingProcessing && processingStage != ProcessingStage.ClausesDetected && processingStage != ProcessingStage.ClausesCategorized && processingStage != ProcessingStage.DocumentClassified)
                    || outcome != IngestionOutcome.Accepted
                    || rejectionReason is not null)
                {
                    throw new DomainValidationException("Invalid accepted document state during rehydration.");
                }

                break;
            case IngestionState.Rejected:
                if (processingStage != ProcessingStage.None || outcome != IngestionOutcome.Rejected || rejectionReason is null)
                {
                    throw new DomainValidationException("Invalid rejected document state during rehydration.");
                }

                break;
            case IngestionState.Failed:
                if (processingStage != ProcessingStage.None || outcome != IngestionOutcome.Failed || rejectionReason is null)
                {
                    throw new DomainValidationException("Invalid failed document state during rehydration.");
                }

                break;
            default:
                throw new DomainValidationException("Invalid document state during rehydration.");
        }
    }

    private static void ValidateRevisions(
        IReadOnlyList<DocumentRevision> revisions,
        IngestionState state,
        IngestionOutcome? outcome,
        ProcessingStage processingStage)
    {
        if (state == IngestionState.PendingAcceptance)
        {
            if (revisions.Count != 0)
            {
                throw new DomainValidationException("Pending documents cannot contain revisions.");
            }

            return;
        }

        if (revisions.Count == 0)
        {
            throw new DomainValidationException("Processed documents must contain at least one revision.");
        }

        var expectedVersion = 1;
        foreach (var revision in revisions)
        {
            if (revision.Version != expectedVersion)
            {
                throw new DomainValidationException("Revision versions must be sequential during rehydration.");
            }

            expectedVersion++;
        }

        var lastRevision = revisions[^1];
        if (outcome is not null && lastRevision.Outcome != outcome.Value)
        {
            throw new DomainValidationException("Latest revision outcome does not match document outcome.");
        }

        if (lastRevision.ProcessingStage != processingStage
            && !(processingStage == ProcessingStage.ClausesDetected && lastRevision.ProcessingStage == ProcessingStage.PendingProcessing)
            && !(processingStage == ProcessingStage.ClausesCategorized && lastRevision.ProcessingStage == ProcessingStage.ClausesDetected)
            && !(processingStage == ProcessingStage.DocumentClassified && lastRevision.ProcessingStage == ProcessingStage.ClausesCategorized))
        {
            throw new DomainValidationException("Latest revision processing stage does not match document stage.");
        }
    }

    public void RecordDetectedDocumentType(DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);

        if (HasDetectedDocumentType)
        {
            throw new InvalidOperationException("Document type detection can only be recorded once.");
        }

        if (documentType.Value.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Unsupported document type");
        }

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot record document type after the document has been rejected or failed.");
        }

        DetectedDocumentType = documentType;
    }

    public void RecordExtractedText(RawText rawText)
    {
        ArgumentNullException.ThrowIfNull(rawText);

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot record extracted text after the document has been rejected or failed.");
        }

        if (HasExtractedText)
        {
            throw new InvalidOperationException("Extracted text can only be recorded once.");
        }

        ExtractedText = rawText;
    }

    public void RecordNormalizedText(NormalizedText normalizedText)
    {
        ArgumentNullException.ThrowIfNull(normalizedText);

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot record normalized text after the document has been rejected or failed.");
        }

        if (!HasExtractedText)
        {
            throw new InvalidOperationException("Cannot record normalized text before extracted text is available.");
        }

        if (HasNormalizedText)
        {
            throw new InvalidOperationException("Normalized text can only be recorded once.");
        }

        NormalizedText = normalizedText;
    }

    public void FailExtraction(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot fail extraction after the document has been rejected or failed.");
        }

        State = IngestionState.Failed;
        ProcessingStage = ProcessingStage.None;
        Outcome = IngestionOutcome.Failed;
        RejectionReason = new RejectionReason(reason);
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Failed, ProcessingStage.None));
    }

    public void RecordDetectedClauses(IEnumerable<Clause> clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);

        if (!HasNormalizedText)
        {
            throw new InvalidOperationException("Clause detection requires normalized text.");
        }

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot record clauses after the document has been rejected or failed.");
        }

        var clauseList = clauses.ToList();
        if (clauseList.Count == 0)
        {
            throw new DomainValidationException("At least one clause is required.");
        }

        if (HasClauses)
        {
            throw new InvalidOperationException("Clause detection can only be recorded once.");
        }

        foreach (var clause in clauseList)
        {
            ArgumentNullException.ThrowIfNull(clause);
        }

        _clauses.Clear();
        _clauses.AddRange(clauseList);
        ProcessingStage = ProcessingStage.ClausesDetected;
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Accepted, ProcessingStage));
    }

    public void FailClauseDetection(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot fail clause detection after the document has been rejected or failed.");
        }

        State = IngestionState.Failed;
        ProcessingStage = ProcessingStage.None;
        Outcome = IngestionOutcome.Failed;
        RejectionReason = new RejectionReason(reason);
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Failed, ProcessingStage.None));
    }

    public void RecordCategoryAssignments(IEnumerable<ClauseCategoryAssignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);

        if (!HasClauses)
        {
            throw new InvalidOperationException("Clause categorization requires detected clauses.");
        }

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot record category assignments after the document has been rejected or failed.");
        }

        var assignmentList = assignments.ToList();
        if (assignmentList.Count == 0)
        {
            throw new DomainValidationException("At least one category assignment is required.");
        }

        if (HasCategoryAssignments)
        {
            throw new InvalidOperationException("Clause categorization can only be recorded once.");
        }

        foreach (var assignment in assignmentList)
        {
            ArgumentNullException.ThrowIfNull(assignment);
        }

        var detectedClauseIds = _clauses.Select(clause => clause.Id).ToHashSet();
        var invalidAssignments = assignmentList
            .Where(assignment => !detectedClauseIds.Contains(assignment.ClauseId))
            .ToList();

        if (invalidAssignments.Count > 0)
        {
            throw new DomainValidationException("Category assignments must reference detected clauses.");
        }

        _categoryAssignments.Clear();
        _categoryAssignments.AddRange(assignmentList);
        ProcessingStage = ProcessingStage.ClausesCategorized;
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Accepted, ProcessingStage));
    }

    public void FailClauseCategorization(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot fail clause categorization after the document has been rejected or failed.");
        }

        State = IngestionState.Failed;
        ProcessingStage = ProcessingStage.None;
        Outcome = IngestionOutcome.Failed;
        RejectionReason = new RejectionReason(reason);
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Failed, ProcessingStage.None));
    }

    public void RecordDocumentClassification(DocumentClassificationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot record document classification after the document has been rejected or failed.");
        }

        if (HasDocumentClassification)
        {
            throw new InvalidOperationException("Document classification can only be recorded once.");
        }

        _documentClassification = result;
        ProcessingStage = ProcessingStage.DocumentClassified;
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Accepted, ProcessingStage));
    }

    private void RestoreDocumentClassification(DocumentClassificationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _documentClassification = result;
    }

    private void RestoreDocumentSummary(DocumentSummaryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _documentSummary = result;
    }

    private void RestoreDocumentEmbedding(DocumentEmbeddingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _documentEmbedding = result;
    }

    public void FailDocumentClassification(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot fail document classification after the document has been rejected or failed.");
        }

        State = IngestionState.Failed;
        ProcessingStage = ProcessingStage.None;
        Outcome = IngestionOutcome.Failed;
        RejectionReason = new RejectionReason(reason);
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Failed, ProcessingStage.None));
    }

    public void RecordDocumentSummary(DocumentSummaryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot record document summary after the document has been rejected or failed.");
        }

        if (HasDocumentSummary)
        {
            throw new InvalidOperationException("Document summary can only be recorded once.");
        }

        _documentSummary = result;
        ProcessingStage = ProcessingStage.DocumentSummarized;
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Accepted, ProcessingStage));
    }

    public void FailDocumentSummary(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot fail document summary after the document has been rejected or failed.");
        }

        State = IngestionState.Failed;
        ProcessingStage = ProcessingStage.None;
        Outcome = IngestionOutcome.Failed;
        RejectionReason = new RejectionReason(reason);
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Failed, ProcessingStage.None));
    }

    public void RecordDocumentEmbedding(DocumentEmbeddingResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot record document embedding after the document has been rejected or failed.");
        }

        if (HasDocumentEmbedding)
        {
            throw new InvalidOperationException("Document embedding can only be recorded once.");
        }

        _documentEmbedding = result;
        ProcessingStage = ProcessingStage.DocumentEmbedded;
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Accepted, ProcessingStage));
    }

    public void FailDocumentEmbedding(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (State == IngestionState.Rejected || State == IngestionState.Failed)
        {
            throw new InvalidOperationException("Cannot fail document embedding after the document has been rejected or failed.");
        }

        State = IngestionState.Failed;
        ProcessingStage = ProcessingStage.None;
        Outcome = IngestionOutcome.Failed;
        RejectionReason = new RejectionReason(reason);
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Failed, ProcessingStage.None));
    }

    public void RejectForProcessingFailure(RejectionReason rejectionReason)
    {
        ArgumentNullException.ThrowIfNull(rejectionReason);

        if (State != IngestionState.Accepted)
        {
            throw new InvalidOperationException("Only accepted documents can be rejected during processing.");
        }

        State = IngestionState.Rejected;
        ProcessingStage = ProcessingStage.None;
        Outcome = IngestionOutcome.Rejected;
        RejectionReason = rejectionReason;
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Rejected, ProcessingStage.None));
    }

    public string DescribeExtractionFailureReason()
    {
        return RejectionReason?.Value ?? "Extraction failed";
    }

    private void RejectInternal(RejectionReason rejectionReason)
    {
        if (State != IngestionState.PendingAcceptance)
        {
            throw new InvalidOperationException("Only pending acceptance documents can be rejected.");
        }

        State = IngestionState.Rejected;
        ProcessingStage = ProcessingStage.None;
        Outcome = IngestionOutcome.Rejected;
        RejectionReason = rejectionReason;
        _revisions.Add(new DocumentRevision(_revisions.Count + 1, DateTimeOffset.UtcNow, IngestionOutcome.Rejected, ProcessingStage.None));
    }
}
