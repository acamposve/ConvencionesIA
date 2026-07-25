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

        Document.ValidateTenantContext(tenantId);

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

        Document.ValidateTenantContext(tenantId);

        return Document.Reject(id, tenantId, source, format, metadata, provenance, correlationId, idempotencyKey, rejectionReason);
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
}
