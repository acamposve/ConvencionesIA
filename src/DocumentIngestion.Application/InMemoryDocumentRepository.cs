using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly Dictionary<string, Document> _documentsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Document> _documentsByIdempotencyKey = new(StringComparer.OrdinalIgnoreCase);

    public void Save(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _documentsById[document.Id.Value] = document;
        _documentsByIdempotencyKey[BuildIdempotencyKey(document)] = document;
    }

    public Document? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        _documentsById.TryGetValue(id, out var document);
        return document;
    }

    public Document? GetByTenantAndIdempotencyKey(string tenantId, string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        var compositeKey = BuildCompositeKey(tenantId, idempotencyKey);
        _documentsByIdempotencyKey.TryGetValue(compositeKey, out var document);
        return document;
    }

    private static string BuildIdempotencyKey(Document document)
    {
        return BuildCompositeKey(document.TenantId.Value, document.IdempotencyKey.Value);
    }

    private static string BuildCompositeKey(string tenantId, string idempotencyKey)
    {
        return $"{tenantId}:{idempotencyKey}";
    }
}
