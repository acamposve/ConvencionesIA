using DocumentIngestion.Domain;
using System.Text.Json;

namespace DocumentIngestion.Application;

public sealed class FileSystemDocumentRepository : IDocumentRepository
{
    private readonly string _storageDirectory;

    public FileSystemDocumentRepository(string storageDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        _storageDirectory = storageDirectory;
        Directory.CreateDirectory(_storageDirectory);
    }

    public void Save(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var path = GetPath(document.Id.Value);
        var snapshot = CreateSnapshot(document);
        var payload = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, payload);
    }

    public Document? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var path = GetPath(id);
        if (!File.Exists(path))
        {
            return null;
        }

        var payload = File.ReadAllText(path);
        var snapshot = JsonSerializer.Deserialize<DocumentSnapshot>(payload);
        return snapshot is null ? null : ToDocument(snapshot);
    }

    public Document? GetByTenantAndIdempotencyKey(string tenantId, string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        foreach (var file in Directory.GetFiles(_storageDirectory, "*.json"))
        {
            var payload = File.ReadAllText(file);
            var snapshot = JsonSerializer.Deserialize<DocumentSnapshot>(payload);
            if (snapshot is not null && string.Equals(snapshot.TenantId, tenantId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(snapshot.IdempotencyKey, idempotencyKey, StringComparison.OrdinalIgnoreCase))
            {
                return ToDocument(snapshot);
            }
        }

        return null;
    }

    private static DocumentSnapshot CreateSnapshot(Document document)
    {
        return new DocumentSnapshot(
            document.Id.Value,
            document.TenantId.Value,
            document.Source.Value,
            document.Format.Value,
            document.Metadata.FileSizeBytes,
            document.Metadata.MimeType,
            document.Metadata.Language,
            document.Metadata.PageCount,
            document.Metadata.Author,
            document.Metadata.CreationDate,
            document.Provenance.SourceReference,
            document.Provenance.SourceName,
            document.CorrelationId.Value,
            document.IdempotencyKey.Value,
            document.Outcome?.ToString() ?? string.Empty,
            document.RejectionReason?.Value,
            document.ProcessingStage.ToString(),
            document.State.ToString());
    }

    private static Document ToDocument(DocumentSnapshot snapshot)
    {
        var metadata = new DocumentMetadata(
            snapshot.FileSizeBytes,
            snapshot.MimeType,
            snapshot.Language,
            snapshot.PageCount,
            snapshot.Author,
            snapshot.CreationDate);

        var provenance = new Provenance(snapshot.SourceReference, snapshot.SourceName);
        var tenantId = new TenantId(snapshot.TenantId);
        var source = new DocumentSource(snapshot.Source);
        var format = new DocumentFormat(snapshot.Format);
        var correlationId = new CorrelationId(snapshot.CorrelationId);
        var idempotencyKey = new IdempotencyKey(snapshot.IdempotencyKey);
        var documentId = new DocumentId(snapshot.Id);

        if (string.Equals(snapshot.State, IngestionState.Rejected.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Document.Reject(
                documentId,
                tenantId,
                source,
                format,
                metadata,
                provenance,
                correlationId,
                idempotencyKey,
                new RejectionReason(snapshot.RejectionReason ?? "Unknown"));
        }

        return Document.Accept(
            documentId,
            tenantId,
            source,
            format,
            metadata,
            provenance,
            correlationId,
            idempotencyKey);
    }

    private string GetPath(string id)
    {
        return Path.Combine(_storageDirectory, $"{id}.json");
    }

    private sealed record DocumentSnapshot(
        string Id,
        string TenantId,
        string Source,
        string Format,
        long FileSizeBytes,
        string MimeType,
        string? Language,
        int? PageCount,
        string? Author,
        DateTimeOffset? CreationDate,
        string SourceReference,
        string? SourceName,
        string CorrelationId,
        string IdempotencyKey,
        string Outcome,
        string? RejectionReason,
        string ProcessingStage,
        string State);
}
