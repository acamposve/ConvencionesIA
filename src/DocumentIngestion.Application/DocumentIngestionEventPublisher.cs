using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class DocumentIngestionEventPublisher : IIngestionEventPublisher
{
    private readonly List<IngestionAuditRecord> _auditRecords = [];

    public IReadOnlyList<IngestionAuditRecord> AuditRecords => _auditRecords.AsReadOnly();

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

public sealed record IngestionAuditRecord(
    string DocumentId,
    string TenantId,
    string CorrelationId,
    string EventName,
    string EventVersion,
    DateTimeOffset Timestamp);
