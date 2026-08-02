using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public sealed class TenantAwareRepositoryTests
{
    [Fact]
    public void GetAll_ReturnsOnlyDocumentsForRequestedTenant()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "document-ingestion-tests", Guid.NewGuid().ToString("N"), "tenant-scope.sqlite");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        var connectionString = $"Data Source={databasePath};Mode=ReadWriteCreate";
        var repository = new SqliteDocumentRepository(connectionString);

        var first = Document.Accept(
            new DocumentId("tenant-doc-1"),
            new TenantId("tenant-a"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(256, "application/pdf", "en", 1, "Alice", DateTimeOffset.Parse("2024-01-01T00:00:00Z")),
            new Provenance("https://example.com/a.pdf", "Example"),
            new CorrelationId("corr-a"),
            new IdempotencyKey("tenant-a|upload|a"));
        var second = Document.Accept(
            new DocumentId("tenant-doc-2"),
            new TenantId("tenant-b"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(256, "application/pdf", "en", 1, "Bob", DateTimeOffset.Parse("2024-01-01T00:00:00Z")),
            new Provenance("https://example.com/b.pdf", "Example"),
            new CorrelationId("corr-b"),
            new IdempotencyKey("tenant-b|upload|b"));

        repository.Save(first);
        repository.Save(second);

        var documents = repository.GetAll("tenant-a", 1, 10);

        Assert.Single(documents);
        Assert.All(documents, document => Assert.Equal("tenant-a", document.TenantId));
    }
}
