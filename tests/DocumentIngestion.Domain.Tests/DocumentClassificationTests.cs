using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Domain.Tests;

public class DocumentClassificationTests
{
    [Fact]
    public void RecordDocumentClassification_TransitionsDocumentToClassifiedStage()
    {
        var document = Document.Accept(
            new DocumentId("doc-70"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-70"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordDocumentClassification(DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(0.92m)));

        Assert.True(document.HasDocumentClassification);
        Assert.Equal(ProcessingStage.DocumentClassified, document.ProcessingStage);
        Assert.Equal(IngestionState.Accepted, document.State);
        Assert.Equal(IngestionOutcome.Accepted, document.Outcome);
    }

    [Fact]
    public void Rehydrate_AllowsAcceptedDocumentAtDocumentClassifiedStage()
    {
        var revisions = new List<DocumentRevision>
        {
            new(1, new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero), IngestionOutcome.Accepted, ProcessingStage.ClausesCategorized),
            new(2, new DateTimeOffset(2024, 1, 2, 3, 5, 0, TimeSpan.Zero), IngestionOutcome.Accepted, ProcessingStage.DocumentClassified)
        };

        var document = Document.Rehydrate(
            new DocumentId("doc-74"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-74"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"),
            IngestionState.Accepted,
            ProcessingStage.DocumentClassified,
            IngestionOutcome.Accepted,
            null,
            null,
            new RawText("raw text"),
            null,
            revisions);

        Assert.Equal(IngestionState.Accepted, document.State);
        Assert.Equal(ProcessingStage.DocumentClassified, document.ProcessingStage);
        Assert.Equal(IngestionOutcome.Accepted, document.Outcome);
    }

    [Fact]
    public void Rehydrate_RestoresDocumentClassificationWithoutCreatingRevisionOrChangingState()
    {
        var revisions = new List<DocumentRevision>
        {
            new(1, new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero), IngestionOutcome.Accepted, ProcessingStage.DocumentClassified)
        };

        var classification = DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(0.92m));

        var document = Document.Rehydrate(
            new DocumentId("doc-75"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-75"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"),
            IngestionState.Accepted,
            ProcessingStage.DocumentClassified,
            IngestionOutcome.Accepted,
            null,
            null,
            new RawText("raw text"),
            null,
            revisions,
            null,
            null,
            classification);

        Assert.True(document.HasDocumentClassification);
        Assert.Equal("NDA", document.DocumentClassification!.ClassificationCode.Value);
        Assert.Equal(0.92m, document.DocumentClassification.ConfidenceScore.Value);
        Assert.Equal(1, document.Revisions.Count);
        Assert.Equal(IngestionState.Accepted, document.State);
        Assert.Equal(ProcessingStage.DocumentClassified, document.ProcessingStage);
    }

    [Fact]
    public void RecordDocumentClassification_RejectsEmptyClassificationCode()
    {
        var exception = Assert.Throws<DomainValidationException>(() => DocumentClassificationResult.Create(
            new DocumentClassificationCode(" "),
            new ConfidenceScore(0.92m)));

        Assert.Equal("Classification code is required.", exception.Message);
    }

    [Fact]
    public void RecordDocumentClassification_AllowsBoundaryConfidenceScores()
    {
        var result = DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(1m));

        Assert.Equal("NDA", result.ClassificationCode.Value);
        Assert.Equal(1m, result.ConfidenceScore.Value);
    }

    [Fact]
    public void RecordDocumentClassification_RejectsOutOfRangeConfidenceScore()
    {
        var exception = Assert.Throws<DomainValidationException>(() => DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(1.2m)));

        Assert.Equal("Confidence score must be between 0.0 and 1.0.", exception.Message);
    }

    [Fact]
    public void RecordDocumentClassification_RejectsDuplicateClassificationRecords()
    {
        var document = Document.Accept(
            new DocumentId("doc-72"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-72"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordDocumentClassification(DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(0.92m)));

        var exception = Assert.Throws<InvalidOperationException>(() => document.RecordDocumentClassification(DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(0.92m))));

        Assert.Equal("Document classification can only be recorded once.", exception.Message);
    }

    [Fact]
    public void FailDocumentClassification_TransitionsDocumentToFailedState()
    {
        var document = Document.Accept(
            new DocumentId("doc-71"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-71"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.FailDocumentClassification("Document classification failed");

        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(ProcessingStage.None, document.ProcessingStage);
        Assert.Equal(IngestionOutcome.Failed, document.Outcome);
        Assert.Equal("Document classification failed", document.RejectionReason?.Value);
    }

    [Fact]
    public void FailDocumentClassification_RejectsBlankReason()
    {
        var document = Document.Accept(
            new DocumentId("doc-73"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-73"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        Assert.Throws<ArgumentException>(() => document.FailDocumentClassification(" "));
    }
}
