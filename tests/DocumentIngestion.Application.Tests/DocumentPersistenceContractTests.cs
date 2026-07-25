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
            new List<DocumentRevisionPersistenceContract>
            {
                new(1, new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero), "Accepted", "PendingProcessing")
            });

        Assert.Equal("doc-1", contract.Id);
        Assert.Equal("tenant-1", contract.TenantId);
        Assert.Equal("Accepted", contract.Outcome);
        Assert.Equal("PendingProcessing", contract.ProcessingStage);
        Assert.Single(contract.Revisions);
        Assert.Equal(1, contract.Revisions[0].Version);
    }
}
