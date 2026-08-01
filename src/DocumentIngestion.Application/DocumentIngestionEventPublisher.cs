using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class DocumentIngestionEventPublisher : IIngestionEventPublisher
{
    private readonly List<IngestionAuditRecord> _auditRecords = [];
    private readonly List<object> _domainEvents = [];

    public IReadOnlyList<IngestionAuditRecord> AuditRecords => _auditRecords.AsReadOnly();
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    public void Publish(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Outcome != IngestionOutcome.Accepted)
        {
            return;
        }

        var domainEvent = new DocumentIngestionCompletedEvent(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1");

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "DocumentIngestionCompleted",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishTextExtracted(Document document, string extractionStrategy, int textLength)
    {
        ArgumentNullException.ThrowIfNull(document);

        var domainEvent = new TextExtractedEvent(
            document.Id.Value,
            document.TenantId.Value,
            document.DetectedDocumentType?.Value ?? "Unknown",
            extractionStrategy,
            textLength,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1");

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "TextExtracted",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishTextExtractionFailed(Document document, string reason)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reason);

        var domainEvent = new TextExtractionFailedEvent(
            document.Id.Value,
            document.TenantId.Value,
            reason,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1");

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "TextExtractionFailed",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishTextNormalized(Document document, string normalizationStrategy, int textLength)
    {
        ArgumentNullException.ThrowIfNull(document);

        var domainEvent = new TextNormalizedEvent(
            document.Id.Value,
            document.TenantId.Value,
            normalizationStrategy,
            textLength,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1");

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "TextNormalized",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishTextNormalizationFailed(Document document, string reason)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reason);

        var domainEvent = new TextNormalizationFailedEvent(
            document.Id.Value,
            document.TenantId.Value,
            reason,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1");

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "TextNormalizationFailed",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishClauseDetectionCompleted(Document document, int clauseCount)
    {
        ArgumentNullException.ThrowIfNull(document);

        var domainEvent = new ClauseDetectionCompletedEvent(
            document.Id.Value,
            document.TenantId.Value,
            clauseCount,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1");

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "ClauseDetectionCompleted",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishClauseDetectionFailed(Document document, string reason)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reason);

        var domainEvent = new ClauseDetectionFailedEvent(
            document.Id.Value,
            document.TenantId.Value,
            reason,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1");

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "ClauseDetectionFailed",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishClauseCategorizationCompleted(Document document, int clauseCount)
    {
        ArgumentNullException.ThrowIfNull(document);

        var domainEvent = new ClauseCategorizationCompletedEvent(
            document.Id.Value,
            document.TenantId.Value,
            clauseCount,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1");

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "ClauseCategorizationCompleted",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishClauseCategorizationFailed(Document document, string reason)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reason);

        var domainEvent = new ClauseCategorizationFailedEvent(
            document.Id.Value,
            document.TenantId.Value,
            reason,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1");

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "ClauseCategorizationFailed",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishDocumentClassificationCompleted(Document document, string classificationCode, decimal confidenceScore)
    {
        ArgumentNullException.ThrowIfNull(document);

        var domainEvent = new DocumentClassificationCompletedEvent(
            document.Id.Value,
            document.TenantId.Value,
            classificationCode,
            confidenceScore,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1",
            document.Revisions.Count);

        _domainEvents.Add(domainEvent);

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "DocumentClassificationCompleted",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishDocumentClassificationFailed(Document document, string reason)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reason);

        var domainEvent = new DocumentClassificationFailedEvent(
            document.Id.Value,
            document.TenantId.Value,
            reason,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1",
            document.Revisions.Count);

        _domainEvents.Add(domainEvent);

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "DocumentClassificationFailed",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishDocumentSummaryCompleted(Document document, string summaryText)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(summaryText);

        var domainEvent = new DocumentSummaryCompletedEvent(
            document.Id.Value,
            document.TenantId.Value,
            summaryText,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1",
            document.Revisions.Count);

        _domainEvents.Add(domainEvent);

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "DocumentSummaryCompleted",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishDocumentSummaryFailed(Document document, string reason)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reason);

        var domainEvent = new DocumentSummaryFailedEvent(
            document.Id.Value,
            document.TenantId.Value,
            reason,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1",
            document.Revisions.Count);

        _domainEvents.Add(domainEvent);

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "DocumentSummaryFailed",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishDocumentEmbeddingCompleted(Document document, IReadOnlyList<decimal> embeddingValues)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(embeddingValues);

        var domainEvent = new DocumentEmbeddingCompletedEvent(
            document.Id.Value,
            document.TenantId.Value,
            embeddingValues.ToList(),
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1",
            document.Revisions.Count);

        _domainEvents.Add(domainEvent);

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "DocumentEmbeddingCompleted",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishDocumentEmbeddingFailed(Document document, string reason)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reason);

        var domainEvent = new DocumentEmbeddingFailedEvent(
            document.Id.Value,
            document.TenantId.Value,
            reason,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1",
            document.Revisions.Count);

        _domainEvents.Add(domainEvent);

        _auditRecords.Add(new IngestionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "DocumentEmbeddingFailed",
            domainEvent.Version,
            domainEvent.Timestamp));
    }
}

public sealed record DocumentIngestionCompletedEvent(
    string DocumentId,
    string TenantId,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version);

public sealed record TextExtractedEvent(
    string DocumentId,
    string TenantId,
    string DocumentType,
    string ExtractionStrategy,
    int TextLength,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version);

public sealed record TextExtractionFailedEvent(
    string DocumentId,
    string TenantId,
    string Reason,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version);

public sealed record TextNormalizedEvent(
    string DocumentId,
    string TenantId,
    string NormalizationStrategy,
    int TextLength,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version);

public sealed record TextNormalizationFailedEvent(
    string DocumentId,
    string TenantId,
    string Reason,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version);

public sealed record ClauseDetectionCompletedEvent(
    string DocumentId,
    string TenantId,
    int ClauseCount,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version);

public sealed record ClauseDetectionFailedEvent(
    string DocumentId,
    string TenantId,
    string Reason,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version);

public sealed record ClauseCategorizationCompletedEvent(
    string DocumentId,
    string TenantId,
    int ClauseCount,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version);

public sealed record ClauseCategorizationFailedEvent(
    string DocumentId,
    string TenantId,
    string Reason,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version);

public sealed record DocumentClassificationCompletedEvent(
    string DocumentId,
    string TenantId,
    string ClassificationCode,
    decimal ConfidenceScore,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version,
    int RevisionNumber);

public sealed record DocumentClassificationFailedEvent(
    string DocumentId,
    string TenantId,
    string Reason,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version,
    int RevisionNumber);

public sealed record DocumentSummaryCompletedEvent(
    string DocumentId,
    string TenantId,
    string SummaryText,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version,
    int RevisionNumber);

public sealed record DocumentSummaryFailedEvent(
    string DocumentId,
    string TenantId,
    string Reason,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version,
    int RevisionNumber);

public sealed record DocumentEmbeddingCompletedEvent(
    string DocumentId,
    string TenantId,
    IReadOnlyList<decimal> EmbeddingValues,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version,
    int RevisionNumber);

public sealed record DocumentEmbeddingFailedEvent(
    string DocumentId,
    string TenantId,
    string Reason,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version,
    int RevisionNumber);

public sealed record IngestionAuditRecord(
    string DocumentId,
    string TenantId,
    string CorrelationId,
    string EventName,
    string EventVersion,
    DateTimeOffset Timestamp);
