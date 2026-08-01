using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocumentIngestionEventPublisherTests
{
    [Fact]
    public void Publish_CreatesAuditRecordForAcceptedDocument()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var document = Document.Accept(
            new DocumentId("doc-1"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-1"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        publisher.Publish(document);

        Assert.Single(publisher.AuditRecords);
        Assert.Equal("DocumentIngestionCompleted", publisher.AuditRecords[0].EventName);
        Assert.Equal("v1", publisher.AuditRecords[0].EventVersion);
    }

    [Fact]
    public void Publish_DoesNotCreateAuditRecordForRejectedDocument()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var document = Document.Reject(
            new DocumentId("doc-2"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-2"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"),
            new RejectionReason("Unsupported format"));

        publisher.Publish(document);

        Assert.Empty(publisher.AuditRecords);
    }

    [Fact]
    public void PublishTextExtracted_CreatesAuditRecordForSuccessfulExtraction()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var document = Document.Accept(
            new DocumentId("doc-3"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-3"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));
        document.RecordDetectedDocumentType(new DocumentType("Pdf"));

        publisher.PublishTextExtracted(document, "Pdf", 12);

        Assert.Single(publisher.AuditRecords);
        Assert.Equal("TextExtracted", publisher.AuditRecords[0].EventName);
        Assert.Equal("v1", publisher.AuditRecords[0].EventVersion);
    }

    [Fact]
    public void PublishTextExtractionFailed_CreatesAuditRecordForFailedExtraction()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var document = Document.Accept(
            new DocumentId("doc-4"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-4"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        publisher.PublishTextExtractionFailed(document, "boom");

        Assert.Single(publisher.AuditRecords);
        Assert.Equal("TextExtractionFailed", publisher.AuditRecords[0].EventName);
        Assert.Equal("v1", publisher.AuditRecords[0].EventVersion);
    }

    [Fact]
    public void PublishTextNormalized_CreatesAuditRecordForSuccessfulNormalization()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var document = Document.Accept(
            new DocumentId("doc-5"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-5"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        publisher.PublishTextNormalized(document, "OcrCleanup", 10);

        Assert.Single(publisher.AuditRecords);
        Assert.Equal("TextNormalized", publisher.AuditRecords[0].EventName);
        Assert.Equal("v1", publisher.AuditRecords[0].EventVersion);
    }

    [Fact]
    public void PublishTextNormalizationFailed_CreatesAuditRecordForFailedNormalization()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var document = Document.Accept(
            new DocumentId("doc-6"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-6"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        publisher.PublishTextNormalizationFailed(document, "boom");

        Assert.Single(publisher.AuditRecords);
        Assert.Equal("TextNormalizationFailed", publisher.AuditRecords[0].EventName);
        Assert.Equal("v1", publisher.AuditRecords[0].EventVersion);
    }

    [Fact]
    public void PublishClauseDetectionCompleted_CreatesAuditRecordForSuccessfulDetection()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var document = Document.Accept(
            new DocumentId("doc-7"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-7"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        publisher.PublishClauseDetectionCompleted(document, 2);

        Assert.Single(publisher.AuditRecords);
        Assert.Equal("ClauseDetectionCompleted", publisher.AuditRecords[0].EventName);
        Assert.Equal("v1", publisher.AuditRecords[0].EventVersion);
        Assert.Equal("tenant-1", publisher.AuditRecords[0].TenantId);
        Assert.Equal("corr-7", publisher.AuditRecords[0].CorrelationId);
    }

    [Fact]
    public void PublishClauseDetectionFailed_CreatesAuditRecordForFailedDetection()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var document = Document.Accept(
            new DocumentId("doc-8"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-8"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        publisher.PublishClauseDetectionFailed(document, "boom");

        Assert.Single(publisher.AuditRecords);
        Assert.Equal("ClauseDetectionFailed", publisher.AuditRecords[0].EventName);
        Assert.Equal("v1", publisher.AuditRecords[0].EventVersion);
        Assert.Equal("tenant-1", publisher.AuditRecords[0].TenantId);
        Assert.Equal("corr-8", publisher.AuditRecords[0].CorrelationId);
    }

    [Fact]
    public void PublishClauseCategorizationCompleted_CreatesAuditRecordForSuccessfulCategorization()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var document = Document.Accept(
            new DocumentId("doc-9"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-9"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        publisher.PublishClauseCategorizationCompleted(document, 2);

        Assert.Single(publisher.AuditRecords);
        Assert.Equal("ClauseCategorizationCompleted", publisher.AuditRecords[0].EventName);
        Assert.Equal("v1", publisher.AuditRecords[0].EventVersion);
        Assert.Equal("tenant-1", publisher.AuditRecords[0].TenantId);
        Assert.Equal("corr-9", publisher.AuditRecords[0].CorrelationId);
    }

    [Fact]
    public void PublishClauseCategorizationFailed_CreatesAuditRecordForFailedCategorization()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var document = Document.Accept(
            new DocumentId("doc-10"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-10"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        publisher.PublishClauseCategorizationFailed(document, "boom");

        Assert.Single(publisher.AuditRecords);
        Assert.Equal("ClauseCategorizationFailed", publisher.AuditRecords[0].EventName);
        Assert.Equal("v1", publisher.AuditRecords[0].EventVersion);
        Assert.Equal("tenant-1", publisher.AuditRecords[0].TenantId);
        Assert.Equal("corr-10", publisher.AuditRecords[0].CorrelationId);
    }
}
