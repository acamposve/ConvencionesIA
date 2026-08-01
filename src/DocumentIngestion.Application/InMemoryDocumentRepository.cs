using DocumentIngestion.Domain;
using System.Collections.Concurrent;

namespace DocumentIngestion.Application;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<string, Document> _documentsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Document> _documentsByIdempotencyKey = new(StringComparer.OrdinalIgnoreCase);

    public void Save(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _documentsById[document.Id.Value] = document;
        _documentsByIdempotencyKey[BuildIdempotencyKey(document)] = document;
    }

    public bool TryCreate(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var compositeKey = BuildCompositeKey(document.TenantId.Value, document.IdempotencyKey.Value);
        if (!_documentsByIdempotencyKey.TryAdd(compositeKey, document))
        {
            return false;
        }

        _documentsById[document.Id.Value] = document;
        return true;
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
