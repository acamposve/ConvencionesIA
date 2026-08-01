using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class CategorizeClausesUseCase
{
    private readonly IClauseCategorizationService _clauseCategorizationService;
    private readonly Action<string>? _logger;
    private readonly IIngestionEventPublisher _eventPublisher;

    public CategorizeClausesUseCase(IClauseCategorizationService clauseCategorizationService)
        : this(clauseCategorizationService, null, null)
    {
    }

    public CategorizeClausesUseCase(IClauseCategorizationService clauseCategorizationService, Action<string>? logger)
        : this(clauseCategorizationService, logger, null)
    {
    }

    public CategorizeClausesUseCase(IClauseCategorizationService clauseCategorizationService, Action<string>? logger, IIngestionEventPublisher? eventPublisher)
    {
        _clauseCategorizationService = clauseCategorizationService ?? throw new ArgumentNullException(nameof(clauseCategorizationService));
        _logger = logger;
        _eventPublisher = eventPublisher ?? new NullIngestionEventPublisher();
    }

    public Document Execute(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.State == IngestionState.Rejected || document.State == IngestionState.Failed)
        {
            _logger?.Invoke($"ClauseCategorization|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Skipped|ClauseCount=0|ProcessingTimeMs=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        if (document.HasCategoryAssignments)
        {
            _logger?.Invoke($"ClauseCategorization|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Existing|ClauseCount={document.CategoryAssignments.Count}|ProcessingTimeMs=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            if (!document.HasClauses)
            {
                throw new InvalidOperationException("Clause categorization requires detected clauses.");
            }

            var result = _clauseCategorizationService.Categorize(document.Clauses);
            document.RecordCategoryAssignments(result.Assignments);
            _eventPublisher.PublishClauseCategorizationCompleted(document, result.Assignments.Count);
            _logger?.Invoke($"ClauseCategorization|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Completed|ClauseCount={result.Assignments.Count}|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }
        catch (Exception ex)
        {
            var failureReason = "Clause categorization failed";
            document.FailClauseCategorization(failureReason);
            _eventPublisher.PublishClauseCategorizationFailed(document, failureReason);
            _logger?.Invoke($"ClauseCategorization|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Failed|ClauseCount=0|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            _logger?.Invoke($"ClauseCategorizationFailure|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|ErrorType={ex.GetType().Name}|CorrelationId={document.CorrelationId.Value}");
            throw new InvalidOperationException(failureReason, ex);
        }
    }

    private sealed class NullIngestionEventPublisher : IIngestionEventPublisher
    {
        public void Publish(Document document) { }
        public void PublishTextExtracted(Document document, string extractionStrategy, int textLength) { }
        public void PublishTextExtractionFailed(Document document, string reason) { }
        public void PublishTextNormalized(Document document, string normalizationStrategy, int textLength) { }
        public void PublishTextNormalizationFailed(Document document, string reason) { }
        public void PublishClauseDetectionCompleted(Document document, int clauseCount) { }
        public void PublishClauseDetectionFailed(Document document, string reason) { }
        public void PublishClauseCategorizationCompleted(Document document, int clauseCount) { }
        public void PublishClauseCategorizationFailed(Document document, string reason) { }
    }
}
