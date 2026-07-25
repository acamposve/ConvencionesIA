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
}

public sealed record DocumentIngestionCompletedEvent(
    string DocumentId,
    string TenantId,
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
