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
    public void Execute_RecordsDetectedDocumentTypeWhenDetectionSucceeds()
    {
        var repository = new TestDocumentRepository();
        var publisher = new TestIngestionEventPublisher();
        var detectionService = new TestDocumentTypeDetectionService("application/pdf");
        var useCase = new DetectDocumentTypeUseCase(detectionService);

        var document = Document.Accept(
            new DocumentId("doc-12"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-12"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.True(result.HasDetectedDocumentType);
        Assert.Equal("Pdf", result.DetectedDocumentType?.Value);
    }

    [Fact]
    public void Execute_RejectsWhenDetectionReturnsUnknownType()
    {
        var detectionService = new TestDocumentTypeDetectionService("application/octet-stream");
        var useCase = new DetectDocumentTypeUseCase(detectionService);

        var document = Document.Accept(
            new DocumentId("doc-13"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/octet-stream", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-13"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<DomainValidationException>(() => useCase.Execute(document));

        Assert.Equal("Unsupported document type", ex.Message);
    }

    [Fact]
    public void Execute_LogsDetectionOutcomeWhenLoggerIsProvided()
    {
        var detectionService = new TestDocumentTypeDetectionService("application/pdf");
        var messages = new List<string>();
        var useCase = new DetectDocumentTypeUseCase(detectionService, messages.Add);

        var document = Document.Accept(
            new DocumentId("doc-14"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-14"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        useCase.Execute(document);

        Assert.Contains(messages, message => message.Contains("DocumentTypeDetection|DocumentId=doc-14"));
        Assert.Contains(messages, message => message.Contains("DetectedType=Pdf"));
    }

    [Fact]
    public void Execute_DoesNotReprocessADocumentThatAlreadyHasDetectedType()
    {
        var detectionService = new CountingDocumentTypeDetectionService();
        var useCase = new DetectDocumentTypeUseCase(detectionService);

        var document = Document.Accept(
            new DocumentId("doc-15"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-15"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordDetectedDocumentType(new DocumentType("Pdf"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.Equal(0, detectionService.CallCount);
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
    }

    private sealed class TestDocumentTypeDetectionService : IDocumentTypeDetectionService
    {
        private readonly string _mimeType;

        public TestDocumentTypeDetectionService(string mimeType)
        {
            _mimeType = mimeType;
        }

        public DocumentType Detect(string mimeType)
        {
            return mimeType switch
            {
                "application/pdf" => new DocumentType("Pdf"),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => new DocumentType("Docx"),
                "image/png" => new DocumentType("Png"),
                _ => new DocumentType("Unknown")
            };
        }
    }

    private sealed class CountingDocumentTypeDetectionService : IDocumentTypeDetectionService
    {
        public int CallCount { get; private set; }

        public DocumentType Detect(string mimeType)
        {
            CallCount++;
            return new DocumentType("Pdf");
        }
    }
}
