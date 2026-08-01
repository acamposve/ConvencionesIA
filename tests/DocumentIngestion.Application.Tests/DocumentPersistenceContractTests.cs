using DocumentIngestion.Application;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocumentPersistenceContractTests
{
    [Fact]
    public void Contract_ContainsTenantAwareDocumentStateAndRevisionHistory()
    {
        var contract = new DocumentPersistenceContract(
            "doc-1",
            "tenant-1",
            "Upload",
            "PDF",
            2048,
            "application/pdf",
            "en",
            3,
            "Alice",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "https://example.com/file.pdf",
            "Example",
            "corr-1",
            "tenant-1|upload|https://example.com/file.pdf",
            "Accepted",
            null,
            "PendingProcessing",
            "Accepted",
            "Pdf",
            "raw text",
            "normalized text",
            new List<DocumentRevisionPersistenceContract>
            {
                new(1, new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero), "Accepted", "PendingProcessing")
            });

        Assert.Equal("doc-1", contract.Id);
        Assert.Equal("tenant-1", contract.TenantId);
        Assert.Equal("Accepted", contract.Outcome);
        Assert.Equal("PendingProcessing", contract.ProcessingStage);
        Assert.Equal("Pdf", contract.DetectedDocumentType);
        Assert.Equal("raw text", contract.ExtractedText);
        Assert.Equal("normalized text", contract.NormalizedText);
        Assert.Single(contract.Revisions);
        Assert.Equal(1, contract.Revisions[0].Version);
    }

    [Fact]
    public void Contract_CanRepresentDocumentEmbeddingState()
    {
        var contract = new DocumentPersistenceContract(
            "doc-2",
            "tenant-1",
            "Upload",
            "PDF",
            2048,
            "application/pdf",
            "en",
            3,
            "Alice",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "https://example.com/file.pdf",
            "Example",
            "corr-2",
            "tenant-1|upload|https://example.com/file.pdf",
            "Accepted",
            null,
            "DocumentEmbedded",
            "Accepted",
            "Pdf",
            "raw text",
            "normalized text",
            new List<DocumentRevisionPersistenceContract>
            {
                new(1, new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero), "Accepted", "DocumentEmbedded")
            },
            null,
            null,
            null,
            null,
            new List<DocumentEmbeddingPersistenceContract>
            {
                new(new List<decimal> { 0.1m, 0.2m, 0.3m })
            });

        Assert.Equal("DocumentEmbedded", contract.ProcessingStage);
        Assert.Single(contract.DocumentEmbeddings);
        Assert.Equal(3, contract.DocumentEmbeddings[0].EmbeddingValues.Count);
        Assert.Equal(0.2m, contract.DocumentEmbeddings[0].EmbeddingValues[1]);
    }
}
