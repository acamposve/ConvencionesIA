using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class IngestDocumentUseCaseTests
{
    [Fact]
    public void Execute_PersistsAndPublishesAcceptedDocument()
    {
        var repository = new TestDocumentRepository();
        var publisher = new TestIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);

        var request = new IngestionRequest(
            "tenant-1",
            "Upload",
            "PDF",
            2048,
            "application/pdf",
            "en",
            3,
            "Example Author",
            DateTimeOffset.Parse("2024-01-01T00:00:00Z"),
            "https://example.com/file.pdf",
            "corr-1",
            "tenant-1|upload|https://example.com/file.pdf");

        var result = useCase.Execute(request);

        Assert.Equal("Accepted", result.State);
        Assert.Equal("PendingProcessing", result.ProcessingStage);
        Assert.Null(result.RejectionReason);
        Assert.NotNull(repository.SavedDocument);
        Assert.NotNull(publisher.PublishedDocument);
    }

    [Fact]
    public void Execute_PersistsRejectedDocumentWhenDomainValidationFails()
    {
        var repository = new TestDocumentRepository();
        var publisher = new TestIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);

        var request = new IngestionRequest(
            "tenant-1",
            "Email",
            "PDF",
            2048,
            "application/pdf",
            "en",
            3,
            "Example Author",
            DateTimeOffset.Parse("2024-01-01T00:00:00Z"),
            "https://example.com/file.pdf",
            "corr-1",
            "tenant-1|upload|https://example.com/file.pdf");

        var result = useCase.Execute(request);

        Assert.Equal("Rejected", result.State);
        Assert.Equal("None", result.ProcessingStage);
        Assert.Equal("Unsupported source", result.RejectionReason);
        Assert.NotNull(repository.SavedDocument);
        Assert.Null(publisher.PublishedDocument);
    }

    [Fact]
    public void Constructor_ThrowsWhenDetectionServiceIsNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new DetectDocumentTypeUseCase(null!));

        Assert.Equal("detectionService", ex.ParamName);
    }

    private sealed class TestDocumentRepository : IDocumentRepository

    {
        public Document? SavedDocument { get; private set; }

        public void Save(Document document) => SavedDocument = document;

        public Document? GetById(string id) => null;

        public Document? GetByTenantAndIdempotencyKey(string tenantId, string idempotencyKey) => null;
    }

    private sealed class TestIngestionEventPublisher : IIngestionEventPublisher
    {
        public Document? PublishedDocument { get; private set; }

        public void Publish(Document document) => PublishedDocument = document;

        public void PublishTextExtracted(Document document, string extractionStrategy, int textLength)
        {
        }

        public void PublishTextExtractionFailed(Document document, string reason)
        {
        }
    }

}
