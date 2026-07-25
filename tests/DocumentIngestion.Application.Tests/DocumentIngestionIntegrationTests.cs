using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocumentIngestionIntegrationTests
{
    [Fact]
    public void EndToEndAcceptedFlow_PersistsDocumentAndPublishesAuditRecord()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);

        var request = CreateValidRequest();
        var result = useCase.Execute(request);

        var persisted = repository.GetById(result.DocumentId);

        Assert.NotNull(persisted);
        Assert.Equal(IngestionState.Accepted, persisted!.State);
        Assert.Equal(ProcessingStage.PendingProcessing, persisted.ProcessingStage);
        Assert.Equal(IngestionOutcome.Accepted, persisted.Outcome);
        Assert.Single(publisher.AuditRecords);
        Assert.Equal("DocumentIngestionCompleted", publisher.AuditRecords[0].EventName);
        Assert.Equal("v1", publisher.AuditRecords[0].EventVersion);
    }

    [Fact]
    public void EndpointAcceptedRequest_UsesSecurityGuardAndPersistsDocument()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = CreateIngestRequestContract();
        var response = endpoint.Handle(request, userId: "user-1", callerTenantId: "tenant-1");

        var persisted = repository.GetById(response.DocumentId);

        Assert.NotNull(persisted);
        Assert.Equal("Accepted", response.Outcome);
        Assert.Equal("PendingProcessing", response.ProcessingStage);
        Assert.Equal("corr-1", response.CorrelationId);
        Assert.Equal("v1", response.Version);
        Assert.Single(publisher.AuditRecords);
    }

    [Fact]
    public void EndpointTenantMismatch_DoesNotPersistOrPublish()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = CreateIngestRequestContract();

        var ex = Assert.Throws<UnauthorizedAccessException>(() => endpoint.Handle(request, userId: "user-1", callerTenantId: "tenant-2"));

        Assert.Contains("Authorization", ex.Message);
        Assert.Null(repository.GetByTenantAndIdempotencyKey("tenant-1", "tenant-1|upload|https://example.com/file.pdf"));
        Assert.Empty(publisher.AuditRecords);
    }

    [Fact]
    public void RejectedRequest_PersistsRejectedDocumentWithoutPublishing()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
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
            "corr-2",
            "tenant-1|upload|https://example.com/file.pdf");

        var result = useCase.Execute(request);
        var persisted = repository.GetById(result.DocumentId);

        Assert.NotNull(persisted);
        Assert.Equal(IngestionState.Rejected, persisted!.State);
        Assert.Equal(IngestionOutcome.Rejected, persisted.Outcome);
        Assert.Equal("Unsupported source", persisted.RejectionReason?.Value);
        Assert.Empty(publisher.AuditRecords);
    }

    private static IngestionRequest CreateValidRequest()
    {
        return new IngestionRequest(
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
    }

    private static IngestDocumentRequestContract CreateIngestRequestContract()
    {
        return new IngestDocumentRequestContract(
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
    }
}
