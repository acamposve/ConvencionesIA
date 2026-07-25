using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocumentIngestionAcceptanceTests
{
    [Fact]
    public void ValidIngestion_IsAcceptedAndTraceable()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
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
        var persisted = repository.GetById(response.DocumentId);

        Assert.Equal("Accepted", response.Outcome);
        Assert.Equal("PendingProcessing", response.ProcessingStage);
        Assert.Equal("corr-1", response.CorrelationId);
        Assert.Equal("v1", response.Version);
        Assert.NotNull(persisted);
        Assert.Equal(IngestionState.Accepted, persisted!.State);
        Assert.Equal(ProcessingStage.PendingProcessing, persisted.ProcessingStage);
        Assert.Equal("https://example.com/file.pdf", persisted.Provenance.SourceReference);
        Assert.Equal("corr-1", persisted.CorrelationId.Value);
        Assert.Single(publisher.AuditRecords);
        Assert.Equal("DocumentIngestionCompleted", publisher.AuditRecords[0].EventName);
    }

    [Fact]
    public void UnsupportedSource_IsRejectedWithBusinessReason()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = new IngestDocumentRequestContract(
            "tenant-1",
            "Email",
            "PDF",
            2048,
            "application/pdf",
            "en",
            3,
            "Alice",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "https://example.com/file.pdf",
            "corr-2",
            "tenant-1|email|https://example.com/file.pdf");

        var response = endpoint.Handle(request, userId: "user-1", callerTenantId: "tenant-1");
        var persisted = repository.GetById(response.DocumentId);

        Assert.Equal("Rejected", response.Outcome);
        Assert.Equal("Unsupported source", response.RejectionReason);
        Assert.Equal("None", response.ProcessingStage);
        Assert.NotNull(persisted);
        Assert.Equal(IngestionState.Rejected, persisted!.State);
        Assert.Equal(ProcessingStage.None, persisted.ProcessingStage);
        Assert.Empty(publisher.AuditRecords);
    }

    [Fact]
    public void UnsupportedFormat_IsRejectedWithBusinessReason()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = new IngestDocumentRequestContract(
            "tenant-1",
            "Upload",
            "TXT",
            2048,
            "text/plain",
            "en",
            3,
            "Alice",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "https://example.com/file.txt",
            "corr-3",
            "tenant-1|upload|https://example.com/file.txt");

        var response = endpoint.Handle(request, userId: "user-1", callerTenantId: "tenant-1");
        var persisted = repository.GetById(response.DocumentId);

        Assert.Equal("Rejected", response.Outcome);
        Assert.Equal("Unsupported format", response.RejectionReason);
        Assert.Equal("None", response.ProcessingStage);
        Assert.NotNull(persisted);
        Assert.Equal(IngestionState.Rejected, persisted!.State);
        Assert.Equal(ProcessingStage.None, persisted.ProcessingStage);
        Assert.Empty(publisher.AuditRecords);
    }

    [Fact]
    public void MissingTenantContext_IsRejectedBeforeProcessingStarts()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
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
            "corr-4",
            "tenant-1|upload|https://example.com/file.pdf");

        var ex = Assert.Throws<ArgumentException>(() => endpoint.Handle(request, userId: "user-1", callerTenantId: " "));

        Assert.Contains("TenantId", ex.Message);
        Assert.Empty(publisher.AuditRecords);
        Assert.Null(repository.GetByTenantAndIdempotencyKey("tenant-1", "tenant-1|upload|https://example.com/file.pdf"));
    }
}
