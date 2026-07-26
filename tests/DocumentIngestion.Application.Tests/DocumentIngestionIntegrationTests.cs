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

    [Fact]
    public void DetectionWorkflow_PersistsDetectedTypeForSupportedMimeType()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var detectionService = new MimeTypeDocumentTypeDetectionService();
        var detectionUseCase = new DetectDocumentTypeUseCase(detectionService);

        var document = Document.Accept(
            new DocumentId("doc-15"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-15"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = detectionUseCase.Execute(document);

        Assert.True(result.HasDetectedDocumentType);
        Assert.Equal("Pdf", result.DetectedDocumentType?.Value);
    }

    [Fact]
    public void DetectionWorkflow_RejectsUnsupportedMimeType()
    {
        var detectionService = new MimeTypeDocumentTypeDetectionService();
        var detectionUseCase = new DetectDocumentTypeUseCase(detectionService);

        var document = Document.Accept(
            new DocumentId("doc-16"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/octet-stream", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-16"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<DomainValidationException>(() => detectionUseCase.Execute(document));

        Assert.Equal("Unsupported document type", ex.Message);
    }

    [Fact]
    public void ExtractTextWorkflow_RecordsExtractedTextAndUpdatesDocumentState()
    {
        var useCase = new ExtractTextUseCase(new PdfTextExtractionService());
        var document = Document.Accept(
            new DocumentId("doc-17"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-17"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.True(result.HasExtractedText);
        Assert.Equal("Sample PDF text from page 1\nSample PDF text from page 2", result.ExtractedText?.Value);
        Assert.Equal(IngestionState.Accepted, result.State);
        Assert.Equal(ProcessingStage.PendingProcessing, result.ProcessingStage);
    }

    [Fact]
    public void ExtractTextWorkflow_RejectsDocumentWhenExtractionFails()
    {
        var useCase = new ExtractTextUseCase(new ThrowingTextExtractionService());
        var document = Document.Accept(
            new DocumentId("doc-18"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-18"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("boom", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(IngestionOutcome.Failed, document.Outcome);
        Assert.Equal("boom", document.RejectionReason?.Value);
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

    private sealed class ThrowingTextExtractionService : ITextExtractionService
    {
        public TextExtractionResult Extract(string content, Document document)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
