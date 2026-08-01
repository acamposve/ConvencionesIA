using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class NormalizeTextUseCase
{
    private readonly ITextNormalizationService _normalizationService;
    private readonly Action<string>? _logger;
    private readonly IIngestionEventPublisher _eventPublisher;

    public NormalizeTextUseCase(ITextNormalizationService normalizationService)
        : this(normalizationService, null, null)
    {
    }

    public NormalizeTextUseCase(ITextNormalizationService normalizationService, Action<string>? logger)
        : this(normalizationService, logger, null)
    {
    }

    public NormalizeTextUseCase(ITextNormalizationService normalizationService, Action<string>? logger, IIngestionEventPublisher? eventPublisher)
    {
        _normalizationService = normalizationService ?? throw new ArgumentNullException(nameof(normalizationService));
        _logger = logger;
        _eventPublisher = eventPublisher ?? new NullIngestionEventPublisher();
    }

    public Document Execute(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.State == IngestionState.Rejected || document.State == IngestionState.Failed)
        {
            _logger?.Invoke($"TextNormalization|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|NormalizationStrategy=Skipped|ProcessingTimeMs=0|OriginalTextLength=0|NormalizedTextLength=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        if (document.HasNormalizedText)
        {
            _logger?.Invoke($"TextNormalization|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|NormalizationStrategy=Existing|ProcessingTimeMs=0|OriginalTextLength={document.ExtractedText?.Value.Length ?? 0}|NormalizedTextLength={document.NormalizedText?.Value.Length ?? 0}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            if (!document.HasExtractedText)
            {
                throw new InvalidOperationException("Cannot normalize text before extracted text is available.");
            }

            var result = _normalizationService.Normalize(document.ExtractedText!.Value, document);
            document.RecordNormalizedText(new NormalizedText(result.NormalizedText));
            _eventPublisher.PublishTextNormalized(document, result.NormalizationStrategy, result.NormalizedText.Length);
            _logger?.Invoke($"TextNormalization|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|NormalizationStrategy={result.NormalizationStrategy}|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|OriginalTextLength={document.ExtractedText!.Value.Length}|NormalizedTextLength={result.NormalizedText.Length}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }
        catch (Exception ex)
        {
            var failureReason = "Normalization failed";
            document.FailExtraction(failureReason);
            _eventPublisher.PublishTextNormalizationFailed(document, failureReason);
            _logger?.Invoke($"TextNormalization|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|NormalizationStrategy=Failed|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|OriginalTextLength={document.ExtractedText?.Value.Length ?? 0}|NormalizedTextLength=0|CorrelationId={document.CorrelationId.Value}");
            _logger?.Invoke($"TextNormalizationFailure|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|ErrorType={ex.GetType().Name}|CorrelationId={document.CorrelationId.Value}");
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

        public void PublishDocumentSummaryCompleted(Document document, string summaryText)
        {
        }

        public void PublishDocumentSummaryFailed(Document document, string reason)
        {
        }
    }
}
