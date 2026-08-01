using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocumentSummaryPersistenceTests
{
    [Fact]
    public void Contract_CanRepresentDocumentSummaryState()
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
            "DocumentSummarized",
            "Accepted",
            "Pdf",
            "raw text",
            "normalized text",
            new List<DocumentRevisionPersistenceContract>
            {
                new(1, new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero), "Accepted", "DocumentClassified")
            },
            null,
            null,
            null,
            new List<DocumentSummaryPersistenceContract>
            {
                new("This document outlines the key obligations and parties.")
            });

        Assert.Equal("DocumentSummarized", contract.ProcessingStage);
        Assert.Single(contract.DocumentSummaries);
        Assert.Equal("This document outlines the key obligations and parties.", contract.DocumentSummaries[0].SummaryText);
    }
}
