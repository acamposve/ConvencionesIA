using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class InMemoryDocumentRepositoryTests
{
    [Fact]
    public void SaveAndGetById_RoundTripsDocument()
    {
        var repository = new InMemoryDocumentRepository();
        var document = Document.Accept(
            new DocumentId("doc-1"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-1"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        repository.Save(document);

        var persisted = repository.GetById("doc-1");

        Assert.NotNull(persisted);
        Assert.Equal("doc-1", persisted!.Id.Value);
    }

    [Fact]
    public void GetByTenantAndIdempotencyKey_FindsPersistedDocument()
    {
        var repository = new InMemoryDocumentRepository();
        var document = Document.Accept(
            new DocumentId("doc-2"),
            new TenantId("tenant-2"),
            new DocumentSource("URL"),
            new DocumentFormat("Image"),
            new DocumentMetadata(1024, "image/png"),
            new Provenance("https://example.com/image.png", "Example"),
            new CorrelationId("corr-2"),
            new IdempotencyKey("tenant-2|url|https://example.com/image.png"));

        repository.Save(document);

        var persisted = repository.GetByTenantAndIdempotencyKey("tenant-2", "tenant-2|url|https://example.com/image.png");

        Assert.NotNull(persisted);
        Assert.Equal("doc-2", persisted!.Id.Value);
    }

    [Fact]
    public void SaveAndGetById_RoundTripsNormalizedText()
    {
        var repository = new InMemoryDocumentRepository();
        var document = Document.Accept(
            new DocumentId("doc-3"),
            new TenantId("tenant-3"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-3"),
            new IdempotencyKey("tenant-3|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("raw text"));
        document.RecordNormalizedText(new NormalizedText("normalized text"));

        repository.Save(document);

        var persisted = repository.GetById("doc-3");

        Assert.NotNull(persisted);
        Assert.True(persisted!.HasNormalizedText);
        Assert.Equal("normalized text", persisted.NormalizedText!.Value);
    }
}
