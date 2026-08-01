using DocumentIngestion.Domain;
using System.Collections.Concurrent;

namespace DocumentIngestion.Application;

public sealed class IngestDocumentUseCase
{
    private static readonly ConcurrentDictionary<string, object> IdempotencyLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDocumentRepository _repository;
    private readonly IIngestionEventPublisher _eventPublisher;
    private readonly DocumentIngestionService _domainService;
    private readonly Action<string>? _logger;

    public IngestDocumentUseCase(IDocumentRepository repository, IIngestionEventPublisher eventPublisher)
        : this(repository, eventPublisher, new DocumentIngestionService(), null)
    {
    }

    public IngestDocumentUseCase(IDocumentRepository repository, IIngestionEventPublisher eventPublisher, DocumentIngestionService domainService)
        : this(repository, eventPublisher, domainService, null)
    {
    }

    public IngestDocumentUseCase(IDocumentRepository repository, IIngestionEventPublisher eventPublisher, DocumentIngestionService domainService, Action<string>? logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _domainService = domainService;
        _logger = logger;
    }

    public IngestionResult Execute(IngestionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var idempotencyKey = new IdempotencyKey(request.IdempotencyKey);
        var lockKey = BuildLockKey(request.TenantId, idempotencyKey.Value);
        var sync = IdempotencyLocks.GetOrAdd(lockKey, static _ => new object());

        lock (sync)
        {
            try
            {
                var existing = GetExistingAcceptedDocument(request.TenantId, idempotencyKey);
                if (existing is not null)
                {
                    _logger?.Invoke($"Duplicate accepted ingestion suppressed for idempotency key {idempotencyKey.Value}.");
                    return new IngestionResult(existing.Id.Value, existing.ProcessingStage.ToString(), existing.State.ToString());
                }

                var document = _domainService.EvaluateAcceptance(
                    new DocumentId(Guid.NewGuid().ToString("N")),
                    new TenantId(request.TenantId),
                    new DocumentSource(request.Source),
                    new DocumentFormat(request.Format),
                    new DocumentMetadata(request.FileSizeBytes, request.MimeType, request.Language, request.PageCount, request.Author, request.CreationDate),
                    new Provenance(request.SourceReference),
                    new CorrelationId(request.CorrelationId),
                    idempotencyKey);

                if (!_repository.TryCreate(document))
                {
                    var createdExisting = _repository.GetByTenantAndIdempotencyKey(request.TenantId, idempotencyKey.Value);
                    if (createdExisting is not null)
                    {
                        _logger?.Invoke($"Duplicate accepted ingestion suppressed for idempotency key {idempotencyKey.Value}.");
                        return new IngestionResult(createdExisting.Id.Value, createdExisting.ProcessingStage.ToString(), createdExisting.State.ToString());
                    }
                }

                _eventPublisher.Publish(document);
                _logger?.Invoke($"Ingestion accepted for correlation {request.CorrelationId}.");

                return new IngestionResult(document.Id.Value, document.ProcessingStage.ToString(), document.State.ToString());
            }
            catch (DomainValidationException ex)
            {
                var rejected = Document.Reject(
                    new DocumentId(Guid.NewGuid().ToString("N")),
                    new TenantId(request.TenantId),
                    new DocumentSource(request.Source),
                    new DocumentFormat(request.Format),
                    new DocumentMetadata(request.FileSizeBytes, request.MimeType, request.Language, request.PageCount, request.Author, request.CreationDate),
                    new Provenance(request.SourceReference),
                    new CorrelationId(request.CorrelationId),
                    idempotencyKey,
                    new RejectionReason(ex.Message));

                _repository.Save(rejected);
                _logger?.Invoke($"Ingestion rejected for correlation {request.CorrelationId}: {ex.Message}");
                return new IngestionResult(rejected.Id.Value, rejected.ProcessingStage.ToString(), rejected.State.ToString(), rejected.RejectionReason?.Value);
            }
        }
    }

    private static string BuildLockKey(string tenantId, string idempotencyKey)
    {
        return $"{tenantId}:{idempotencyKey}";
    }

    private Document? GetExistingAcceptedDocument(string tenantId, IdempotencyKey idempotencyKey)
    {
        var existing = _repository.GetByTenantAndIdempotencyKey(tenantId, idempotencyKey.Value);
        return existing is not null && existing.State == IngestionState.Accepted ? existing : null;
    }
}

public sealed record IngestionRequest(
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
    string CorrelationId,
    string IdempotencyKey);

public sealed record IngestionResult(string DocumentId, string ProcessingStage, string State, string? RejectionReason = null);

public interface IDocumentRepository
{
    void Save(Document document);
    Document? GetById(string id);
    Document? GetByTenantAndIdempotencyKey(string tenantId, string idempotencyKey);
    bool TryCreate(Document document)
    {
        Save(document);
        return true;
    }
}

public interface IIngestionEventPublisher
{
    void Publish(Document document);
    void PublishTextExtracted(Document document, string extractionStrategy, int textLength);
    void PublishTextExtractionFailed(Document document, string reason);
    void PublishTextNormalized(Document document, string normalizationStrategy, int textLength);
    void PublishTextNormalizationFailed(Document document, string reason);
    void PublishClauseDetectionCompleted(Document document, int clauseCount);
    void PublishClauseDetectionFailed(Document document, string reason);
    void PublishClauseCategorizationCompleted(Document document, int clauseCount);
    void PublishClauseCategorizationFailed(Document document, string reason);
    void PublishDocumentClassificationCompleted(Document document, string classificationCode, decimal confidenceScore);
    void PublishDocumentClassificationFailed(Document document, string reason);
    void PublishDocumentSummaryCompleted(Document document, string summaryText);
    void PublishDocumentSummaryFailed(Document document, string reason);
    void PublishDocumentEmbeddingCompleted(Document document, IReadOnlyList<decimal> embeddingValues);
    void PublishDocumentEmbeddingFailed(Document document, string reason);
}
