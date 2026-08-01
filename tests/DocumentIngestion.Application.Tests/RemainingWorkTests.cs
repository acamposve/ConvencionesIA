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
    public async Task Execute_ConcurrentDuplicateRequests_CreateSingleAcceptedDocument()
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

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => useCase.Execute(request)))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var distinctDocumentIds = results.Select(result => result.DocumentId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        Assert.Single(distinctDocumentIds);
        Assert.All(results, result => Assert.Equal("Accepted", result.State));
        Assert.Single(publisher.AuditRecords);
    }

    [Fact]
    public void FileSystemRepository_RoundTripsPendingDocument()
    {
        var storageDirectory = CreateStorageDirectory();
        var repository = new FileSystemDocumentRepository(storageDirectory);
        var document = CreatePendingDocument("doc-pending");

        repository.Save(document);

        var reloadedRepository = new FileSystemDocumentRepository(storageDirectory);
        var persisted = reloadedRepository.GetById(document.Id.Value);

        AssertEquivalent(document, persisted);
    }

    [Fact]
    public void FileSystemRepository_RoundTripsAcceptedDocumentWithDetectedTypeAndText()
    {
        var storageDirectory = CreateStorageDirectory();
        var repository = new FileSystemDocumentRepository(storageDirectory);
        var document = Document.Accept(
            new DocumentId("doc-accepted"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en", 3, "Alice", DateTimeOffset.Parse("2024-01-01T00:00:00Z")),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-accepted"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordDetectedDocumentType(new DocumentType("Pdf"));
        document.RecordExtractedText(new RawText("raw text"));
        document.RecordNormalizedText(new NormalizedText("normalized text"));

        repository.Save(document);

        var reloadedRepository = new FileSystemDocumentRepository(storageDirectory);
        var persisted = reloadedRepository.GetById(document.Id.Value);

        AssertEquivalent(document, persisted);
    }

    [Fact]
    public void FileSystemRepository_RoundTripsRejectedDocument()
    {
        var storageDirectory = CreateStorageDirectory();
        var repository = new FileSystemDocumentRepository(storageDirectory);
        var document = Document.Accept(
            new DocumentId("doc-rejected"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-rejected"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordDetectedDocumentType(new DocumentType("Pdf"));
        document.RejectForProcessingFailure(new RejectionReason("Unsupported document type"));

        repository.Save(document);

        var reloadedRepository = new FileSystemDocumentRepository(storageDirectory);
        var persisted = reloadedRepository.GetById(document.Id.Value);

        AssertEquivalent(document, persisted);
    }

    [Fact]
    public void FileSystemRepository_RoundTripsFailedDocument()
    {
        var storageDirectory = CreateStorageDirectory();
        var repository = new FileSystemDocumentRepository(storageDirectory);
        var document = Document.Accept(
            new DocumentId("doc-failed"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-failed"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordDetectedDocumentType(new DocumentType("Pdf"));
        document.RecordExtractedText(new RawText("raw text"));
        document.FailExtraction("Normalization failed");

        repository.Save(document);

        var reloadedRepository = new FileSystemDocumentRepository(storageDirectory);
        var persisted = reloadedRepository.GetById(document.Id.Value);

        AssertEquivalent(document, persisted);
    }

    [Fact]
    public void FileSystemRepository_IdempotencyLookup_RebuildsMissingIndex()
    {
        var storageDirectory = CreateStorageDirectory();
        var repository = new FileSystemDocumentRepository(storageDirectory);
        var document = Document.Accept(
            new DocumentId("doc-index"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-index"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        repository.Save(document);

        var indexedRepository = new FileSystemDocumentRepository(storageDirectory);
        var fromIndex = indexedRepository.GetByTenantAndIdempotencyKey("tenant-1", "tenant-1|upload|https://example.com/file.pdf");
        AssertEquivalent(document, fromIndex);

        var indexPath = Path.Combine(storageDirectory, "idempotency-index.json");
        Assert.True(File.Exists(indexPath));
        File.Delete(indexPath);

        var rebuiltIndexRepository = new FileSystemDocumentRepository(storageDirectory);
        var afterRebuild = rebuiltIndexRepository.GetByTenantAndIdempotencyKey("tenant-1", "tenant-1|upload|https://example.com/file.pdf");

        AssertEquivalent(document, afterRebuild);
        Assert.True(File.Exists(indexPath));
    }

    private static string CreateStorageDirectory()
    {
        return Path.Combine(Path.GetTempPath(), "document-ingestion-tests", Guid.NewGuid().ToString("N"));
    }

    private static Document CreatePendingDocument(string id)
    {
        return new Document(
            new DocumentId(id),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId($"corr-{id}"),
            new IdempotencyKey($"tenant-1|upload|{id}"));
    }

    private static void AssertEquivalent(Document expected, Document? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Id.Value, actual!.Id.Value);
        Assert.Equal(expected.TenantId.Value, actual.TenantId.Value);
        Assert.Equal(expected.Source.Value, actual.Source.Value);
        Assert.Equal(expected.Format.Value, actual.Format.Value);
        Assert.Equal(expected.Metadata.FileSizeBytes, actual.Metadata.FileSizeBytes);
        Assert.Equal(expected.Metadata.MimeType, actual.Metadata.MimeType);
        Assert.Equal(expected.Metadata.Language, actual.Metadata.Language);
        Assert.Equal(expected.Metadata.PageCount, actual.Metadata.PageCount);
        Assert.Equal(expected.Metadata.Author, actual.Metadata.Author);
        Assert.Equal(expected.Metadata.CreationDate, actual.Metadata.CreationDate);
        Assert.Equal(expected.Provenance.SourceReference, actual.Provenance.SourceReference);
        Assert.Equal(expected.Provenance.SourceName, actual.Provenance.SourceName);
        Assert.Equal(expected.CorrelationId.Value, actual.CorrelationId.Value);
        Assert.Equal(expected.IdempotencyKey.Value, actual.IdempotencyKey.Value);
        Assert.Equal(expected.State, actual.State);
        Assert.Equal(expected.ProcessingStage, actual.ProcessingStage);
        Assert.Equal(expected.Outcome, actual.Outcome);
        Assert.Equal(expected.RejectionReason?.Value, actual.RejectionReason?.Value);
        Assert.Equal(expected.DetectedDocumentType?.Value, actual.DetectedDocumentType?.Value);
        Assert.Equal(expected.ExtractedText?.Value, actual.ExtractedText?.Value);
        Assert.Equal(expected.NormalizedText?.Value, actual.NormalizedText?.Value);
        Assert.Equal(expected.Revisions.Count, actual.Revisions.Count);

        for (var i = 0; i < expected.Revisions.Count; i++)
        {
            Assert.Equal(expected.Revisions[i].Version, actual.Revisions[i].Version);
            Assert.Equal(expected.Revisions[i].Timestamp, actual.Revisions[i].Timestamp);
            Assert.Equal(expected.Revisions[i].Outcome, actual.Revisions[i].Outcome);
            Assert.Equal(expected.Revisions[i].ProcessingStage, actual.Revisions[i].ProcessingStage);
        }
    }
}
