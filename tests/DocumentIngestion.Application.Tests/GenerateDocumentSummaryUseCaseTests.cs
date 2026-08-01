using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class GenerateDocumentSummaryUseCaseTests
{
    [Fact]
    public void Execute_AddsSummaryWhenDocumentHasProcessingEvidence()
    {
        var document = Document.Accept(
            new DocumentId("doc-90"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-90"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("This is the extracted text."));
        document.RecordDocumentClassification(DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(0.92m)));

        var useCase = new GenerateDocumentSummaryUseCase();
        var result = useCase.Execute(document);

        Assert.True(result.HasDocumentSummary);
        Assert.Equal(ProcessingStage.DocumentSummarized, result.ProcessingStage);
        Assert.Equal(IngestionState.Accepted, result.State);
    }

    [Fact]
    public void Execute_FailsWhenNoEvidenceAvailable()
    {
        var document = Document.Accept(
            new DocumentId("doc-91"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-91"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var useCase = new GenerateDocumentSummaryUseCase();

        var exception = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Document summary failed", exception.Message);
        Assert.Equal(IngestionState.Failed, document.State);
    }
}
