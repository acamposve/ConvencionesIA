using System.Collections.Generic;

namespace DocumentIngestion.Domain;

public sealed class Document
{
    private readonly List<DocumentRevision> _revisions = [];

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
    public bool HasDetectedDocumentType => DetectedDocumentType is not null;
    public bool HasExtractedText => ExtractedText is not null;
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
