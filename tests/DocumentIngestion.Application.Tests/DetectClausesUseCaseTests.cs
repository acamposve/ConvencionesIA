using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DetectClausesUseCaseTests
{
    [Fact]
    public void Execute_RecordsDetectedClausesAndPublishesCompletionEvent()
    {
        var publisher = new TestIngestionEventPublisher();
        var useCase = new DetectClausesUseCase(new TestClauseDetectionService(), null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-31"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-31"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("clause one"));
        document.RecordNormalizedText(new NormalizedText("clause one"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.True(result.HasClauses);
        Assert.Equal(ProcessingStage.ClausesDetected, result.ProcessingStage);
        Assert.Equal(1, publisher.CompletedCount);
        Assert.Equal(0, publisher.FailedCount);
    }

    [Fact]
    public void Execute_FailsDocumentWhenDetectionServiceThrows()
    {
        var publisher = new TestIngestionEventPublisher();
        var useCase = new DetectClausesUseCase(new ThrowingClauseDetectionService(), null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-32"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-32"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("clause one"));
        document.RecordNormalizedText(new NormalizedText("clause one"));

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Clause detection failed", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(0, publisher.CompletedCount);
        Assert.Equal(1, publisher.FailedCount);
    }

    [Fact]
    public void Execute_LogsStructuredMetricsWithoutExposingContentOnSuccess()
    {
        var messages = new List<string>();
        var useCase = new DetectClausesUseCase(new TestClauseDetectionService(), messages.Add, null);
        var document = Document.Accept(
            new DocumentId("doc-33"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-33"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("sensitive clause content"));
        document.RecordNormalizedText(new NormalizedText("sensitive clause content"));

        useCase.Execute(document);

        Assert.Contains(messages, message => message.Contains("ClauseDetection|")
            && message.Contains("DocumentId=doc-33")
            && message.Contains("TenantId=tenant-1")
            && message.Contains("Outcome=Completed")
            && message.Contains("ClauseCount=1")
            && message.Contains("CorrelationId=corr-33")
            && !message.Contains("sensitive clause content"));
    }

    [Fact]
    public void Execute_LogsFailureWithoutExposingContentOnException()
    {
        var messages = new List<string>();
        var useCase = new DetectClausesUseCase(new ThrowingClauseDetectionService(), messages.Add, null);
        var document = Document.Accept(
            new DocumentId("doc-34"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-34"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("sensitive clause content"));
        document.RecordNormalizedText(new NormalizedText("sensitive clause content"));

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Clause detection failed", ex.Message);
        Assert.Contains(messages, message => message.Contains("ClauseDetectionFailure|")
            && message.Contains("ErrorType=InvalidOperationException")
            && message.Contains("CorrelationId=corr-34")
            && !message.Contains("sensitive clause content"));
    }

    private sealed class TestClauseDetectionService : IClauseDetectionService
    {
        public ClauseDetectionResult Detect(string normalizedText)
        {
            return new ClauseDetectionResult(new[]
            {
                new DetectedClause(1, null, "clause one", 0, 10)
            });
        }
    }

    private sealed class ThrowingClauseDetectionService : IClauseDetectionService
    {
        public ClauseDetectionResult Detect(string normalizedText)
        {
            throw new InvalidOperationException("boom");
        }
    }

    private sealed class TestIngestionEventPublisher : IIngestionEventPublisher
    {
        public int CompletedCount { get; private set; }
        public int FailedCount { get; private set; }

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
            CompletedCount++;
        }

        public void PublishClauseDetectionFailed(Document document, string reason)
        {
            FailedCount++;
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

        public void PublishDocumentEmbeddingCompleted(Document document, IReadOnlyList<decimal> embeddingValues)
        {
        }

        public void PublishDocumentEmbeddingFailed(Document document, string reason)
        {
        }
    }
}
