using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocumentClassificationPersistenceTests
{
    [Fact]
    public void Contract_CanRepresentDocumentClassificationState()
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
            "DocumentClassified",
            "Accepted",
            "Pdf",
            "raw text",
            "normalized text",
            new List<DocumentRevisionPersistenceContract>
            {
                new(1, new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero), "Accepted", "PendingProcessing")
            },
            null,
            null,
            new List<DocumentClassificationPersistenceContract>
            {
                new("NDA", 0.92m)
            });

        Assert.Equal("DocumentClassified", contract.ProcessingStage);
        Assert.Single(contract.DocumentClassifications);
        Assert.Equal("NDA", contract.DocumentClassifications[0].ClassificationCode);
        Assert.Equal(0.92m, contract.DocumentClassifications[0].ConfidenceScore);
    }
}
