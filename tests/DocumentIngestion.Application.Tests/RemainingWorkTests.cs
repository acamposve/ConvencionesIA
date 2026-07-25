using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class RemainingWorkTests
{
    [Fact]
    public void Execute_ReturnsExistingAcceptedDocumentForDuplicateIdempotencyKey()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
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

        var first = useCase.Execute(request);
        var second = useCase.Execute(request);

        Assert.Equal(first.DocumentId, second.DocumentId);
        Assert.Equal("Accepted", second.State);
        Assert.Equal("PendingProcessing", second.ProcessingStage);
        Assert.Single(publisher.AuditRecords);
    }

    [Fact]
    public void FileSystemRepository_PersistsAndReloadsDocument()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), "document-ingestion-tests", Guid.NewGuid().ToString("N"));
        var repository = new FileSystemDocumentRepository(storageDirectory);
        var document = Document.Accept(
            new DocumentId("doc-123"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-123"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        repository.Save(document);

        var reloadedRepository = new FileSystemDocumentRepository(storageDirectory);
        var persisted = reloadedRepository.GetById(document.Id.Value);

        Assert.NotNull(persisted);
        Assert.Equal(document.Id.Value, persisted!.Id.Value);
        Assert.Equal(document.TenantId.Value, persisted.TenantId.Value);
    }
}
