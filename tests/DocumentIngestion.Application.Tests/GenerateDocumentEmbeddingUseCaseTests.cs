using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class GenerateDocumentEmbeddingUseCaseTests
{
    [Fact]
    public void Execute_AddsEmbeddingWhenDocumentHasProcessingEvidence()
    {
        var document = Document.Accept(
            new DocumentId("doc-100"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-100"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("This is the extracted text for embedding."));
        document.RecordDocumentClassification(DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(0.92m)));

        var useCase = new GenerateDocumentEmbeddingUseCase();
        var result = useCase.Execute(document);

        Assert.True(result.HasDocumentEmbedding);
        Assert.Equal(ProcessingStage.DocumentEmbedded, result.ProcessingStage);
        Assert.Equal(IngestionState.Accepted, result.State);
        Assert.NotEmpty(result.DocumentEmbedding!.EmbeddingVector.Values);
    }

    [Fact]
    public void Execute_FailsWhenNoEvidenceAvailable()
    {
        var document = Document.Accept(
            new DocumentId("doc-101"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-101"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var useCase = new GenerateDocumentEmbeddingUseCase();

        var exception = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Document embedding failed", exception.Message);
        Assert.Equal(IngestionState.Failed, document.State);
    }

    [Fact]
    public void Execute_ProducesDeterministicEmbeddingForEquivalentEvidence()
    {
        var firstDocument = CreateDocument("doc-102", "The contract mentions payment terms and liability clauses.");
        var secondDocument = CreateDocument("doc-103", "The contract mentions payment terms and liability clauses.");

        var useCase = new GenerateDocumentEmbeddingUseCase();
        var firstResult = useCase.Execute(firstDocument);
        var secondResult = useCase.Execute(secondDocument);

        Assert.Equal(firstResult.DocumentEmbedding!.EmbeddingVector.Values, secondResult.DocumentEmbedding!.EmbeddingVector.Values);
    }

    [Fact]
    public void Execute_DoesNotOverrideExistingEmbedding()
    {
        var document = CreateDocument("doc-104", "The contract mentions payment terms and liability clauses.");
        document.RecordDocumentEmbedding(DocumentEmbeddingResult.Create(new EmbeddingVector([99m, 100m])));

        var useCase = new GenerateDocumentEmbeddingUseCase();
        var result = useCase.Execute(document);

        Assert.Equal(new[] { 99m, 100m }, result.DocumentEmbedding!.EmbeddingVector.Values);
    }

    private static Document CreateDocument(string id, string extractedText)
    {
        var document = Document.Accept(
            new DocumentId(id),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId($"corr-{id}"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText(extractedText));
        document.RecordDocumentClassification(DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(0.92m)));

        return document;
    }
}
