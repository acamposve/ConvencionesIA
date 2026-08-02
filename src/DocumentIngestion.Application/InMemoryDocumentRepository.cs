using DocumentIngestion.Domain;
using System.Collections.Concurrent;

namespace DocumentIngestion.Application;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private readonly ConcurrentDictionary<string, Document> _documentsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Document> _documentsByIdempotencyKey = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<DocumentPersistenceContract> GetAll(string? tenantId, int page, int pageSize)
    {
        var documents = _documentsById.Values
            .Where(document => string.IsNullOrWhiteSpace(tenantId) || string.Equals(document.TenantId.Value, tenantId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(document => document.Revisions.LastOrDefault()?.Timestamp ?? DateTimeOffset.MinValue)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(document => new DocumentPersistenceContract(
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
                document.Outcome?.ToString(),
                document.RejectionReason?.Value,
                document.ProcessingStage.ToString(),
                document.State.ToString(),
                document.DetectedDocumentType?.Value,
                document.ExtractedText?.Value,
                document.NormalizedText?.Value,
                document.Revisions.Select(revision => new DocumentRevisionPersistenceContract(
                    revision.Version,
                    revision.Timestamp,
                    revision.Outcome.ToString(),
                    revision.ProcessingStage.ToString())).ToList(),
                document.Clauses.Select(clause => new ClausePersistenceContract(
                    clause.Id.Value,
                    clause.Sequence,
                    clause.NumberLabel?.Value,
                    clause.Text.Value,
                    clause.Span.Start,
                    clause.Span.End)).ToList(),
                document.CategoryAssignments.Select(assignment => new ClauseCategoryAssignmentPersistenceContract(
                    assignment.ClauseId.Value,
                    assignment.CategoryCode.Value,
                    assignment.ConfidenceScore.Value)).ToList(),
                document.DocumentClassification is null ? null : new List<DocumentClassificationPersistenceContract>
                {
                    new(document.DocumentClassification.ClassificationCode.Value, document.DocumentClassification.ConfidenceScore.Value)
                },
                document.DocumentSummary is null ? null : new List<DocumentSummaryPersistenceContract>
                {
                    new(document.DocumentSummary.SummaryText.Value)
                },
                document.DocumentEmbedding is null ? null : new List<DocumentEmbeddingPersistenceContract>
                {
                    new(document.DocumentEmbedding.EmbeddingVector.Values.ToList())
                }))
            .ToList();

        return documents;
    }

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
