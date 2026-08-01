using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocumentIngestionEndpointTests
{
    [Fact]
    public void Handle_ReturnsAcceptedResponseForValidRequest()
    {
        var repository = new TestDocumentRepository();
        var publisher = new TestIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

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

        var response = endpoint.Handle(request, userId: "user-1", callerTenantId: "tenant-1");

        Assert.Equal("Accepted", response.Outcome);
        Assert.Equal("PendingProcessing", response.ProcessingStage);
        Assert.Equal("corr-1", response.CorrelationId);
        Assert.Equal("v1", response.Version);
    }

    [Fact]
    public void Handle_RejectsRequestWithMissingTenantId()
    {
        var repository = new TestDocumentRepository();
        var publisher = new TestIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = new IngestDocumentRequestContract(
            " ",
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

        var ex = Assert.Throws<ArgumentException>(() => endpoint.Handle(request));
        Assert.Contains("TenantId", ex.Message);
    }

    [Fact]
    public void Handle_RejectsMissingAuthentication()
    {
        var repository = new TestDocumentRepository();
        var publisher = new TestIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

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

        var ex = Assert.Throws<UnauthorizedAccessException>(() => endpoint.Handle(request, userId: null));
        Assert.Contains("Authentication", ex.Message);
    }

    [Fact]
    public void Handle_RejectsMismatchedRequestTenantAgainstCallerTenantContext()
    {
        var repository = new TestDocumentRepository();
        var publisher = new TestIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = new IngestDocumentRequestContract(
            "tenant-2",
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

        var ex = Assert.Throws<UnauthorizedAccessException>(() => endpoint.Handle(request, userId: "user-1", callerTenantId: "tenant-3"));
        Assert.Contains("Authorization", ex.Message);
    }

    [Fact]
    public void Handle_RejectsMissingCallerTenantContext()
    {
        var repository = new TestDocumentRepository();
        var publisher = new TestIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

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

        var ex = Assert.Throws<UnauthorizedAccessException>(() => endpoint.Handle(request, userId: "user-1", callerTenantId: null));
        Assert.Contains("Tenant context", ex.Message);
    }

    [Fact]
    public void Handle_RejectsTenantSpoofingAttempt()
    {
        var repository = new TestDocumentRepository();
        var publisher = new TestIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = new IngestDocumentRequestContract(
            "tenant-spoofed",
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
            "tenant-spoofed|upload|https://example.com/file.pdf");

        var ex = Assert.Throws<UnauthorizedAccessException>(() => endpoint.Handle(request, userId: "user-1", callerTenantId: "tenant-real"));
        Assert.Contains("Authorization", ex.Message);
    }

    private sealed class TestDocumentRepository : IDocumentRepository
    {
        public void Save(Document document)
        {
        }

        public Document? GetById(string id) => null;

        public Document? GetByTenantAndIdempotencyKey(string tenantId, string idempotencyKey) => null;
    }

    private sealed class TestIngestionEventPublisher : IIngestionEventPublisher
    {
        public void Publish(Document document)
        {
        }

        public void PublishTextExtracted(Document document, string extractionStrategy, int textLength)
        {
        }

        public void PublishTextExtractionFailed(Document document, string reason)
        {
        }

        public void PublishTextNormalized(Document document, string normalizationStrategy, int textLength)
        {
        }

        public void PublishTextNormalizationFailed(Document document, string reason)
        {
        }

        public void PublishClauseDetectionCompleted(Document document, int clauseCount)
        {
        }

        public void PublishClauseDetectionFailed(Document document, string reason)
        {
        }

        public void PublishClauseCategorizationCompleted(Document document, int clauseCount)
        {
        }

        public void PublishClauseCategorizationFailed(Document document, string reason)
        {
        }
    }
}
