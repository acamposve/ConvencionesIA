using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class ClassifyDocumentUseCaseTests
{
    [Fact]
    public void Execute_RecordsClassificationAndPublishesCompletionEvent()
    {
        var publisher = new TestIngestionEventPublisher();
        var useCase = new ClassifyDocumentUseCase(null, null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-80"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-80"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("This is a contract."));
        document.RecordNormalizedText(new NormalizedText("This is a contract."));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.True(result.HasDocumentClassification);
        Assert.Equal(ProcessingStage.DocumentClassified, result.ProcessingStage);
        Assert.Equal(1, publisher.CompletedCount);
        Assert.Equal(0, publisher.FailedCount);
    }

    [Fact]
    public void Execute_FailsDocumentWhenClassificationCannotBeDetermined()
    {
        var publisher = new TestIngestionEventPublisher();
        var useCase = new ClassifyDocumentUseCase(null, null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-81"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-81"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Document classification failed", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(0, publisher.CompletedCount);
        Assert.Equal(1, publisher.FailedCount);
    }

    private sealed class TestIngestionEventPublisher : IIngestionEventPublisher
    {
        public int CompletedCount { get; private set; }
        public int FailedCount { get; private set; }

        public void Publish(Document document) { }
        public void PublishTextExtracted(Document document, string extractionStrategy, int textLength) { }
        public void PublishTextExtractionFailed(Document document, string reason) { }
        public void PublishTextNormalized(Document document, string normalizationStrategy, int textLength) { }
        public void PublishTextNormalizationFailed(Document document, string reason) { }
        public void PublishClauseDetectionCompleted(Document document, int clauseCount) { }
        public void PublishClauseDetectionFailed(Document document, string reason) { }
        public void PublishClauseCategorizationCompleted(Document document, int clauseCount) { }
        public void PublishClauseCategorizationFailed(Document document, string reason) { }
        public void PublishDocumentClassificationCompleted(Document document, string classificationCode, decimal confidenceScore) { CompletedCount++; }
        public void PublishDocumentClassificationFailed(Document document, string reason) { FailedCount++; }
        public void PublishDocumentSummaryCompleted(Document document, string summaryText) { }
        public void PublishDocumentSummaryFailed(Document document, string reason) { }
        public void PublishDocumentEmbeddingCompleted(Document document, IReadOnlyList<decimal> embeddingValues) { }
        public void PublishDocumentEmbeddingFailed(Document document, string reason) { }
    }
}
