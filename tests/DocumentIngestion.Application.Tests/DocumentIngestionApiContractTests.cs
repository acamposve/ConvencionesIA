using DocumentIngestion.Application;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocumentIngestionApiContractTests
{
    [Fact]
    public void Contract_UsesVersionedRouteAndExpectedMetadata()
    {
        Assert.Equal("/api/v1/documents/ingestion", DocumentIngestionApiContract.Route);
        Assert.Equal("v1", DocumentIngestionApiContract.Version);
        Assert.Equal("Authentication is required for this operation.", DocumentIngestionApiContract.AuthenticationRequirement);
        Assert.Equal("Authorization must ensure the caller can ingest documents for the specified tenant.", DocumentIngestionApiContract.AuthorizationRequirement);
    }

    [Fact]
    public void RequestContract_ContainsTenantContextAndTraceabilityFields()
    {
        var request = new IngestDocumentRequestContract(
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
            "corr-1",
            "tenant-1|upload|https://example.com/file.pdf");

        Assert.Equal("tenant-1", request.TenantId);
        Assert.Equal("Upload", request.Source);
        Assert.Equal("PDF", request.Format);
        Assert.Equal("https://example.com/file.pdf", request.SourceReference);
        Assert.Equal("corr-1", request.CorrelationId);
        Assert.Equal("tenant-1|upload|https://example.com/file.pdf", request.IdempotencyKey);
    }

    [Fact]
    public void ResponseContract_PreservesOutcomeAndVersioningDetails()
    {
        var response = new IngestDocumentResponseContract(
            "doc-1",
            "Accepted",
            "PendingProcessing",
            null,
            "corr-1",
            DateTimeOffset.Parse("2024-01-02T03:04:05Z"));

        Assert.Equal("doc-1", response.DocumentId);
        Assert.Equal("Accepted", response.Outcome);
        Assert.Equal("PendingProcessing", response.ProcessingStage);
        Assert.Null(response.RejectionReason);
        Assert.Equal("corr-1", response.CorrelationId);
        Assert.Equal("v1", response.Version);
    }
}
