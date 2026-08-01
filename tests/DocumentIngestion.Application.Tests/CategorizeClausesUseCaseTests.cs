using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class CategorizeClausesUseCaseTests
{
    [Fact]
    public void Execute_RecordsCategoryAssignmentsAndPublishesCompletionEvent()
    {
        var publisher = new TestIngestionEventPublisher();
        var useCase = new CategorizeClausesUseCase(new TestClauseCategorizationService(), null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-50"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-50"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("Clause one."));
        document.RecordNormalizedText(new NormalizedText("Clause one."));
        document.RecordDetectedClauses(new[]
        {
            Clause.Create(
                ClauseId.CreateDeterministic("doc-50", 1, 1),
                1,
                new ClauseText("Clause one."),
                new ClauseSpan(0, 12))
        });

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.True(result.HasCategoryAssignments);
        Assert.Equal(ProcessingStage.ClausesCategorized, result.ProcessingStage);
        Assert.Equal(1, publisher.CompletedCount);
        Assert.Equal(0, publisher.FailedCount);
    }

    [Fact]
    public void Execute_FailsDocumentWhenCategorizationServiceThrows()
    {
        var publisher = new TestIngestionEventPublisher();
        var useCase = new CategorizeClausesUseCase(new ThrowingClauseCategorizationService(), null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-51"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-51"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("Clause one."));
        document.RecordNormalizedText(new NormalizedText("Clause one."));
        document.RecordDetectedClauses(new[]
        {
            Clause.Create(
                ClauseId.CreateDeterministic("doc-51", 1, 1),
                1,
                new ClauseText("Clause one."),
                new ClauseSpan(0, 12))
        });

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Clause categorization failed", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(0, publisher.CompletedCount);
        Assert.Equal(1, publisher.FailedCount);
    }

    private sealed class TestClauseCategorizationService : IClauseCategorizationService
    {
        public ClauseCategorizationResult Categorize(IReadOnlyList<Clause> clauses)
        {
            return new ClauseCategorizationResult(new[]
            {
                ClauseCategoryAssignment.Create(
                    ClauseId.CreateDeterministic("doc-50", 1, 1),
                    new ClauseCategoryCode("Obligation"),
                    new ConfidenceScore(0.95m))
            });
        }
    }

    private sealed class ThrowingClauseCategorizationService : IClauseCategorizationService
    {
        public ClauseCategorizationResult Categorize(IReadOnlyList<Clause> clauses)
        {
            throw new InvalidOperationException("boom");
        }
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
        public void PublishClauseCategorizationCompleted(Document document, int clauseCount) { CompletedCount++; }
        public void PublishClauseCategorizationFailed(Document document, string reason) { FailedCount++; }
        public void PublishDocumentClassificationCompleted(Document document, string classificationCode, decimal confidenceScore) { }
        public void PublishDocumentClassificationFailed(Document document, string reason) { }
        public void PublishDocumentSummaryCompleted(Document document, string summaryText) { }
        public void PublishDocumentSummaryFailed(Document document, string reason) { }
        public void PublishDocumentEmbeddingCompleted(Document document, IReadOnlyList<decimal> embeddingValues) { }
        public void PublishDocumentEmbeddingFailed(Document document, string reason) { }
    }
}
