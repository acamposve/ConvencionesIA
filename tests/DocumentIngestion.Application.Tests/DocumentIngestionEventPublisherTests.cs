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
}
