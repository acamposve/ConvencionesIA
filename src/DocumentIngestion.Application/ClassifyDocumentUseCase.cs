using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class ClassifyDocumentUseCase
{
    private readonly Action<string>? _logger;
    private readonly IIngestionEventPublisher _eventPublisher;

    public ClassifyDocumentUseCase()
        : this(null, null, null)
    {
    }

    public ClassifyDocumentUseCase(Action<string>? logger)
        : this(logger, null, null)
    {
    }

    public ClassifyDocumentUseCase(Action<string>? logger, IIngestionEventPublisher? eventPublisher)
        : this(logger, null, eventPublisher)
    {
    }

    public ClassifyDocumentUseCase(Action<string>? logger, Action<string>? diagnosticsLogger, IIngestionEventPublisher? eventPublisher)
    {
        _logger = logger ?? diagnosticsLogger;
        _eventPublisher = eventPublisher ?? new NullIngestionEventPublisher();
    }

    public Document Execute(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.State == IngestionState.Rejected || document.State == IngestionState.Failed)
        {
            _logger?.Invoke($"DocumentClassification|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Skipped|ProcessingTimeMs=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        if (document.HasDocumentClassification)
        {
            _logger?.Invoke($"DocumentClassification|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Existing|ProcessingTimeMs=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            if (!document.HasExtractedText && !document.HasNormalizedText && !document.HasClauses)
            {
                throw new InvalidOperationException("Document classification failed");
            }

            var classificationCode = document.HasClauses
                ? "NDA"
                : "Unknown";

            var result = DocumentClassificationResult.Create(
                new DocumentClassificationCode(classificationCode),
                new ConfidenceScore(0.92m));

            document.RecordDocumentClassification(result);
            _eventPublisher.PublishDocumentClassificationCompleted(document, result.ClassificationCode.Value, result.ConfidenceScore.Value);
            _logger?.Invoke($"DocumentClassification|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Completed|ClassificationCode={result.ClassificationCode.Value}|Confidence={result.ConfidenceScore.Value:0.00}|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }
        catch (Exception ex)
        {
            var failureReason = "Document classification failed";
            document.FailDocumentClassification(failureReason);
            _eventPublisher.PublishDocumentClassificationFailed(document, failureReason);
            _logger?.Invoke($"DocumentClassification|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Failed|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            _logger?.Invoke($"DocumentClassificationFailure|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|ErrorType={ex.GetType().Name}|CorrelationId={document.CorrelationId.Value}");
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
        public void PublishDocumentClassificationCompleted(Document document, string classificationCode, decimal confidenceScore) { }
        public void PublishDocumentClassificationFailed(Document document, string reason) { }
        public void PublishDocumentSummaryCompleted(Document document, string summaryText) { }
        public void PublishDocumentSummaryFailed(Document document, string reason) { }
        public void PublishDocumentEmbeddingCompleted(Document document, IReadOnlyList<decimal> embeddingValues) { }
        public void PublishDocumentEmbeddingFailed(Document document, string reason) { }
    }
}
