using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class GenerateDocumentEmbeddingUseCase
{
    private readonly Action<string>? _logger;
    private readonly IIngestionEventPublisher _eventPublisher;

    public GenerateDocumentEmbeddingUseCase()
        : this(null, null, null)
    {
    }

    public GenerateDocumentEmbeddingUseCase(Action<string>? logger)
        : this(logger, null, null)
    {
    }

    public GenerateDocumentEmbeddingUseCase(Action<string>? logger, IIngestionEventPublisher? eventPublisher)
        : this(logger, null, eventPublisher)
    {
    }

    public GenerateDocumentEmbeddingUseCase(Action<string>? logger, Action<string>? diagnosticsLogger, IIngestionEventPublisher? eventPublisher)
    {
        _logger = logger ?? diagnosticsLogger;
        _eventPublisher = eventPublisher ?? new NullIngestionEventPublisher();
    }

    public Document Execute(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.State == IngestionState.Rejected || document.State == IngestionState.Failed)
        {
            _logger?.Invoke($"DocumentEmbedding|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Skipped|ProcessingTimeMs=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        if (document.HasDocumentEmbedding)
        {
            _logger?.Invoke($"DocumentEmbedding|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Existing|ProcessingTimeMs=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            if (!document.HasExtractedText && !document.HasNormalizedText && !document.HasDocumentClassification && !document.HasDocumentSummary)
            {
                throw new InvalidOperationException("Document embedding failed");
            }

            var canonicalText = document.NormalizedText?.Value
                ?? document.DocumentSummary?.SummaryText.Value
                ?? document.ExtractedText?.Value
                ?? "processed document";

            var embeddingValues = BuildEmbeddingValues(canonicalText, document.DocumentClassification?.ClassificationCode.Value);
            var result = DocumentEmbeddingResult.Create(new EmbeddingVector(embeddingValues));

            document.RecordDocumentEmbedding(result);
            _eventPublisher.PublishDocumentEmbeddingCompleted(document, result.EmbeddingVector.Values);
            _logger?.Invoke($"DocumentEmbedding|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Completed|VectorLength={result.EmbeddingVector.Values.Count}|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }
        catch (Exception ex)
        {
            var failureReason = "Document embedding failed";
            document.FailDocumentEmbedding(failureReason);
            _eventPublisher.PublishDocumentEmbeddingFailed(document, failureReason);
            _logger?.Invoke($"DocumentEmbedding|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Failed|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            _logger?.Invoke($"DocumentEmbeddingFailure|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|ErrorType={ex.GetType().Name}|CorrelationId={document.CorrelationId.Value}");
            throw new InvalidOperationException(failureReason, ex);
        }
    }

    private static IReadOnlyList<decimal> BuildEmbeddingValues(string canonicalText, string? classificationCode)
    {
        var tokens = canonicalText
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.ToLowerInvariant())
            .ToList();

        var values = new List<decimal>();
        foreach (var token in tokens.Take(8))
        {
            values.Add(token.Length % 7 + 1);
        }

        if (!string.IsNullOrWhiteSpace(classificationCode))
        {
            values.Add(classificationCode.Length % 5 + 1);
        }

        if (values.Count == 0)
        {
            values.Add(1m);
        }

        return values;
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
