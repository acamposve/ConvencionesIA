using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DetectDocumentTypeUseCaseTests
{
    [Fact]
    public void Execute_RecordsDetectedDocumentTypeWhenDetectionSucceeds()
    {
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
        Assert.Equal(IngestionState.Rejected, document.State);
        Assert.Equal(IngestionOutcome.Rejected, document.Outcome);
        Assert.Equal("Unsupported document type", document.RejectionReason?.Value);
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

    private sealed class TestDocumentTypeDetectionService : IDocumentTypeDetectionService
    {
        private readonly string _configuredMimeType;

        public TestDocumentTypeDetectionService(string configuredMimeType)
        {
            _configuredMimeType = configuredMimeType;
        }

        public DocumentType Detect(string mimeType)
        {
            return _configuredMimeType switch
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
