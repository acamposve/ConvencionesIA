using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Domain.Tests;

public class DocumentEmbeddingTests
{
    [Fact]
    public void RecordDocumentEmbedding_TransitionsDocumentToEmbeddedStage()
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

        document.RecordDocumentEmbedding(DocumentEmbeddingResult.Create(
            new EmbeddingVector([0.1m, 0.2m, 0.3m])));

        Assert.True(document.HasDocumentEmbedding);
        Assert.Equal(ProcessingStage.DocumentEmbedded, document.ProcessingStage);
        Assert.Equal(IngestionState.Accepted, document.State);
        Assert.Equal(IngestionOutcome.Accepted, document.Outcome);
    }

    [Fact]
    public void RecordDocumentEmbedding_RejectsEmptyEmbeddingVector()
    {
        var exception = Assert.Throws<DomainValidationException>(() => DocumentEmbeddingResult.Create(
            new EmbeddingVector([])));

        Assert.Equal("Embedding vector is required.", exception.Message);
    }

    [Fact]
    public void RecordDocumentEmbedding_RejectsDuplicateEmbeddingRecording()
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

        document.RecordDocumentEmbedding(DocumentEmbeddingResult.Create(
            new EmbeddingVector([0.1m, 0.2m])));

        var exception = Assert.Throws<InvalidOperationException>(() => document.RecordDocumentEmbedding(
            DocumentEmbeddingResult.Create(new EmbeddingVector([0.3m, 0.4m]))));

        Assert.Equal("Document embedding can only be recorded once.", exception.Message);
    }

    [Fact]
    public void FailDocumentEmbedding_TransitionsDocumentToFailedState()
    {
        var document = Document.Accept(
            new DocumentId("doc-92"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-92"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.FailDocumentEmbedding("Document embedding failed");

        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(ProcessingStage.None, document.ProcessingStage);
        Assert.Equal(IngestionOutcome.Failed, document.Outcome);
        Assert.Equal("Document embedding failed", document.RejectionReason?.Value);
    }
}
