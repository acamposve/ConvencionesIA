using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class DetectClausesUseCase
{
    private readonly IClauseDetectionService _clauseDetectionService;
    private readonly Action<string>? _logger;
    private readonly IIngestionEventPublisher _eventPublisher;

    public DetectClausesUseCase(IClauseDetectionService clauseDetectionService)
        : this(clauseDetectionService, null, null)
    {
    }

    public DetectClausesUseCase(IClauseDetectionService clauseDetectionService, Action<string>? logger)
        : this(clauseDetectionService, logger, null)
    {
    }

    public DetectClausesUseCase(IClauseDetectionService clauseDetectionService, Action<string>? logger, IIngestionEventPublisher? eventPublisher)
    {
        _clauseDetectionService = clauseDetectionService ?? throw new ArgumentNullException(nameof(clauseDetectionService));
        _logger = logger;
        _eventPublisher = eventPublisher ?? new NullIngestionEventPublisher();
    }

    public Document Execute(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.State == IngestionState.Rejected || document.State == IngestionState.Failed)
        {
            _logger?.Invoke($"ClauseDetection|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Skipped|ClauseCount=0|ProcessingTimeMs=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        if (document.HasClauses)
        {
            _logger?.Invoke($"ClauseDetection|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Existing|ClauseCount={document.Clauses.Count}|ProcessingTimeMs=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            if (!document.HasNormalizedText)
            {
                throw new InvalidOperationException("Clause detection requires normalized text.");
            }

            var result = _clauseDetectionService.Detect(document.NormalizedText!.Value);
            var clauses = result.Clauses
                .Select((clause, index) => Clause.Create(
                    ClauseId.CreateDeterministic(document.Id.Value, 1, clause.Sequence),
                    clause.Sequence,
                    new ClauseText(clause.Text),
                    new ClauseSpan(clause.SpanStart, clause.SpanEnd),
                    string.IsNullOrWhiteSpace(clause.NumberLabel) ? null : new ClauseNumberLabel(clause.NumberLabel)))
                .ToList();

            document.RecordDetectedClauses(clauses);
            _eventPublisher.PublishClauseDetectionCompleted(document, clauses.Count);
            _logger?.Invoke($"ClauseDetection|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Completed|ClauseCount={clauses.Count}|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }
        catch (Exception ex)
        {
            var failureReason = "Clause detection failed";
            document.FailClauseDetection(failureReason);
            _eventPublisher.PublishClauseDetectionFailed(document, failureReason);
            _logger?.Invoke($"ClauseDetection|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Failed|ClauseCount=0|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            _logger?.Invoke($"ClauseDetectionFailure|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|ErrorType={ex.GetType().Name}|CorrelationId={document.CorrelationId.Value}");
            throw new InvalidOperationException(failureReason, ex);
        }
    }

    private sealed class NullIngestionEventPublisher : IIngestionEventPublisher
    {
        public void Publish(Document document)
        {
        }

        public void PublishTextExtracted(Document document, string extractionStrategy, int textLength)
        {
        }

        public void PublishTextExtractionFailed(Document document, string reason)
        {
        }

        public void PublishTextNormalized(Document document, string normalizationStrategy, int textLength)
        {
        }

        public void PublishTextNormalizationFailed(Document document, string reason)
        {
        }

        public void PublishClauseDetectionCompleted(Document document, int clauseCount)
        {
        }

        public void PublishClauseDetectionFailed(Document document, string reason)
        {
        }

        public void PublishClauseCategorizationCompleted(Document document, int clauseCount)
        {
        }

        public void PublishClauseCategorizationFailed(Document document, string reason)
        {
        }

        public void PublishDocumentClassificationCompleted(Document document, string classificationCode, decimal confidenceScore)
        {
        }

        public void PublishDocumentClassificationFailed(Document document, string reason)
        {
        }
    }
}
