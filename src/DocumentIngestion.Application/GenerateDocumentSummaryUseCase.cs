using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class GenerateDocumentSummaryUseCase
{
    private readonly Action<string>? _logger;
    private readonly IIngestionEventPublisher _eventPublisher;

    public GenerateDocumentSummaryUseCase()
        : this(null, null, null)
    {
    }

    public GenerateDocumentSummaryUseCase(Action<string>? logger)
        : this(logger, null, null)
    {
    }

    public GenerateDocumentSummaryUseCase(Action<string>? logger, IIngestionEventPublisher? eventPublisher)
        : this(logger, null, eventPublisher)
    {
    }

    public GenerateDocumentSummaryUseCase(Action<string>? logger, Action<string>? diagnosticsLogger, IIngestionEventPublisher? eventPublisher)
    {
        _logger = logger ?? diagnosticsLogger;
        _eventPublisher = eventPublisher ?? new NullIngestionEventPublisher();
    }

    public Document Execute(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.State == IngestionState.Rejected || document.State == IngestionState.Failed)
        {
            _logger?.Invoke($"DocumentSummary|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Skipped|ProcessingTimeMs=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        if (document.HasDocumentSummary)
        {
            _logger?.Invoke($"DocumentSummary|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Existing|ProcessingTimeMs=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            if (!document.HasExtractedText && !document.HasNormalizedText && !document.HasClauses && !document.HasDocumentClassification)
            {
                throw new InvalidOperationException("Document summary failed");
            }

            var canonicalText = document.NormalizedText?.Value
                ?? document.ExtractedText?.Value
                ?? "processed document";

            var summaryText = BuildSummaryText(document, canonicalText);

            var result = DocumentSummaryResult.Create(
                new SummaryText(summaryText));

            document.RecordDocumentSummary(result);
            _eventPublisher.PublishDocumentSummaryCompleted(document, BuildEventSummaryMarker(result.SummaryText.Value));
            _logger?.Invoke($"DocumentSummary|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Completed|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }
        catch (Exception ex)
        {
            var failureReason = "Document summary failed";
            document.FailDocumentSummary(failureReason);
            _eventPublisher.PublishDocumentSummaryFailed(document, failureReason);
            _logger?.Invoke($"DocumentSummary|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|Outcome=Failed|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            _logger?.Invoke($"DocumentSummaryFailure|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|ErrorType={ex.GetType().Name}|CorrelationId={document.CorrelationId.Value}");
            throw new InvalidOperationException(failureReason, ex);
        }
    }

    private static string BuildSummaryText(Document document, string canonicalText)
    {
        var trimmedText = string.Join(" ", canonicalText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Take(24));
        var prefix = document.HasDocumentClassification
            ? $"Summary for {document.DocumentClassification!.ClassificationCode.Value}:"
            : "Summary:";

        return $"{prefix} {trimmedText}".Trim();
    }

    private static string BuildEventSummaryMarker(string summaryText)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(summaryText));
        return $"summary:sha256:{Convert.ToHexString(hash)}";
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
