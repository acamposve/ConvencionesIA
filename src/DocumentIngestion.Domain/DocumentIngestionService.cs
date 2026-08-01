namespace DocumentIngestion.Domain;

public sealed class DocumentIngestionService
{
    public Document EvaluateAcceptance(
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

        return Document.Accept(id, tenantId, source, format, metadata, provenance, correlationId, idempotencyKey);
    }

    public Document EvaluateRejection(
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

        return Document.Reject(id, tenantId, source, format, metadata, provenance, correlationId, idempotencyKey, rejectionReason);
    }
}
