using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class DocumentTypeDetectionEventPublisher
{
    private readonly List<DocumentTypeDetectionAuditRecord> _auditRecords = [];

    public IReadOnlyList<DocumentTypeDetectionAuditRecord> AuditRecords => _auditRecords.AsReadOnly();

    public void PublishSuccessfulDetection(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.DetectedDocumentType is null)
        {
            return;
        }

        var domainEvent = new DocumentTypeDetectedEvent(
            document.Id.Value,
            document.TenantId.Value,
            document.DetectedDocumentType.Value,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1");

        _auditRecords.Add(new DocumentTypeDetectionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "DocumentTypeDetected",
            domainEvent.Version,
            domainEvent.Timestamp));
    }

    public void PublishDetectionFailure(Document document, string reason)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(reason);

        var domainEvent = new DocumentTypeDetectionFailedEvent(
            document.Id.Value,
            document.TenantId.Value,
            reason,
            document.CorrelationId.Value,
            DateTimeOffset.UtcNow,
            "v1");

        _auditRecords.Add(new DocumentTypeDetectionAuditRecord(
            document.Id.Value,
            document.TenantId.Value,
            document.CorrelationId.Value,
            "DocumentTypeDetectionFailed",
            domainEvent.Version,
            domainEvent.Timestamp));
    }
}

public sealed record DocumentTypeDetectedEvent(
    string DocumentId,
    string TenantId,
    string DocumentType,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version);

public sealed record DocumentTypeDetectionFailedEvent(
    string DocumentId,
    string TenantId,
    string Reason,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Version);

public sealed record DocumentTypeDetectionAuditRecord(
    string DocumentId,
    string TenantId,
    string CorrelationId,
    string EventName,
    string EventVersion,
    DateTimeOffset Timestamp);
