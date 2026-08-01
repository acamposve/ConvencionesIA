using DocumentIngestion.Application;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocumentIngestionContractTests
{
    [Fact]
    public void ApiContract_ExposesVersionedRouteAndSecurityRequirements()
    {
        Assert.Equal("/api/v1/documents/ingestion", DocumentIngestionApiContract.Route);
        Assert.Equal("v1", DocumentIngestionApiContract.Version);
        Assert.Equal("Authentication is required for this operation.", DocumentIngestionApiContract.AuthenticationRequirement);
        Assert.Equal("Authorization must ensure the caller can ingest documents for the specified tenant.", DocumentIngestionApiContract.AuthorizationRequirement);
    }

    [Fact]
    public void RequestContract_ContainsTheFieldsRequiredByTheEndpoint()
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
    public void ResponseContract_PreservesOutcomeAndVersionForClients()
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
        Assert.Equal("v1", response.Version);
    }

    [Fact]
    public void CompletedEventContract_UsesVersionedEnvelope()
    {
        var timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var domainEvent = new DocumentIngestionCompletedEvent("doc-1", "tenant-1", "corr-1", timestamp, "v1");

        Assert.Equal("doc-1", domainEvent.DocumentId);
        Assert.Equal("tenant-1", domainEvent.TenantId);
        Assert.Equal("corr-1", domainEvent.CorrelationId);
        Assert.Equal(timestamp, domainEvent.Timestamp);
        Assert.Equal("v1", domainEvent.Version);
    }

    [Fact]
    public void ClauseDetectionCompletedEventContract_ContainsClauseCountAndCorrelationContext()
    {
        var timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var domainEvent = new ClauseDetectionCompletedEvent("doc-1", "tenant-1", 2, "corr-1", timestamp, "v1");

        Assert.Equal("doc-1", domainEvent.DocumentId);
        Assert.Equal("tenant-1", domainEvent.TenantId);
        Assert.Equal(2, domainEvent.ClauseCount);
        Assert.Equal("corr-1", domainEvent.CorrelationId);
        Assert.Equal("v1", domainEvent.Version);
    }

    [Fact]
    public void ClauseDetectionFailedEventContract_ContainsFailureReasonAndCorrelationContext()
    {
        var timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var domainEvent = new ClauseDetectionFailedEvent("doc-1", "tenant-1", "boom", "corr-1", timestamp, "v1");

        Assert.Equal("doc-1", domainEvent.DocumentId);
        Assert.Equal("tenant-1", domainEvent.TenantId);
        Assert.Equal("boom", domainEvent.Reason);
        Assert.Equal("corr-1", domainEvent.CorrelationId);
        Assert.Equal("v1", domainEvent.Version);
    }

    [Fact]
    public void ClauseCategorizationCompletedEventContract_ContainsClauseCountAndCorrelationContext()
    {
        var timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var domainEvent = new ClauseCategorizationCompletedEvent("doc-1", "tenant-1", 2, "corr-1", timestamp, "v1");

        Assert.Equal("doc-1", domainEvent.DocumentId);
        Assert.Equal("tenant-1", domainEvent.TenantId);
        Assert.Equal(2, domainEvent.ClauseCount);
        Assert.Equal("corr-1", domainEvent.CorrelationId);
        Assert.Equal("v1", domainEvent.Version);
    }

    [Fact]
    public void ClauseCategorizationFailedEventContract_ContainsFailureReasonAndCorrelationContext()
    {
        var timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var domainEvent = new ClauseCategorizationFailedEvent("doc-1", "tenant-1", "boom", "corr-1", timestamp, "v1");

        Assert.Equal("doc-1", domainEvent.DocumentId);
        Assert.Equal("tenant-1", domainEvent.TenantId);
        Assert.Equal("boom", domainEvent.Reason);
        Assert.Equal("corr-1", domainEvent.CorrelationId);
        Assert.Equal("v1", domainEvent.Version);
    }

    [Fact]
    public void DocumentClassificationCompletedEventContract_ContainsClassificationMetadataAndCorrelationContext()
    {
        var timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var domainEvent = new DocumentClassificationCompletedEvent("doc-1", "tenant-1", "NDA", 0.92m, "corr-1", timestamp, "v1", 2);

        Assert.Equal("doc-1", domainEvent.DocumentId);
        Assert.Equal("tenant-1", domainEvent.TenantId);
        Assert.Equal("NDA", domainEvent.ClassificationCode);
        Assert.Equal(0.92m, domainEvent.ConfidenceScore);
        Assert.Equal("corr-1", domainEvent.CorrelationId);
        Assert.Equal("v1", domainEvent.Version);
        Assert.Equal(2, domainEvent.RevisionNumber);
    }

    [Fact]
    public void DocumentClassificationFailedEventContract_ContainsFailureReasonAndCorrelationContext()
    {
        var timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var domainEvent = new DocumentClassificationFailedEvent("doc-1", "tenant-1", "boom", "corr-1", timestamp, "v1", 2);

        Assert.Equal("doc-1", domainEvent.DocumentId);
        Assert.Equal("tenant-1", domainEvent.TenantId);
        Assert.Equal("boom", domainEvent.Reason);
        Assert.Equal("corr-1", domainEvent.CorrelationId);
        Assert.Equal("v1", domainEvent.Version);
        Assert.Equal(2, domainEvent.RevisionNumber);
    }

    [Fact]
    public void DocumentSummaryCompletedEventContract_ContainsRedactedSummaryMetadataAndCorrelationContext()
    {
        var timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var domainEvent = new DocumentSummaryCompletedEvent("doc-1", "tenant-1", "summary:sha256:abc123", "corr-1", timestamp, "v1", 2);

        Assert.Equal("doc-1", domainEvent.DocumentId);
        Assert.Equal("tenant-1", domainEvent.TenantId);
        Assert.Equal("summary:sha256:abc123", domainEvent.SummaryText);
        Assert.Equal("corr-1", domainEvent.CorrelationId);
        Assert.Equal("v1", domainEvent.Version);
        Assert.Equal(2, domainEvent.RevisionNumber);
    }

    [Fact]
    public void DocumentSummaryFailedEventContract_ContainsFailureReasonAndCorrelationContext()
    {
        var timestamp = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var domainEvent = new DocumentSummaryFailedEvent("doc-1", "tenant-1", "boom", "corr-1", timestamp, "v1", 2);

        Assert.Equal("doc-1", domainEvent.DocumentId);
        Assert.Equal("tenant-1", domainEvent.TenantId);
        Assert.Equal("boom", domainEvent.Reason);
        Assert.Equal("corr-1", domainEvent.CorrelationId);
        Assert.Equal("v1", domainEvent.Version);
        Assert.Equal(2, domainEvent.RevisionNumber);
    }
}
