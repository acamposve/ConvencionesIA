using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Domain.Tests;

public class DocumentTests
{
    [Fact]
    public void Accept_CreatesAcceptedDocumentWithPendingProcessingStage()
    {
        var document = Document.Accept(
            new DocumentId("doc-1"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-1"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        Assert.Equal(IngestionState.Accepted, document.State);
        Assert.Equal(ProcessingStage.PendingProcessing, document.ProcessingStage);
        Assert.Equal(IngestionOutcome.Accepted, document.Outcome);
        Assert.Null(document.RejectionReason);
        Assert.NotEmpty(document.Revisions);
    }

    [Fact]
    public void Reject_CreatesRejectedDocumentWithReason()
    {
        var document = Document.Reject(
            new DocumentId("doc-2"),
            new TenantId("tenant-1"),
            new DocumentSource("URL"),
            new DocumentFormat("DOCX"),
            new DocumentMetadata(512, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "en"),
            new Provenance("https://example.com/file.docx", "Example"),
            new CorrelationId("corr-2"),
            new IdempotencyKey("tenant-1|url|https://example.com/file.docx"),
            new RejectionReason("Unsupported format"));

        Assert.Equal(IngestionState.Rejected, document.State);
        Assert.Equal(ProcessingStage.None, document.ProcessingStage);
        Assert.Equal(IngestionOutcome.Rejected, document.Outcome);
        Assert.Equal("Unsupported format", document.RejectionReason?.Value);
    }

    [Fact]
    public void Accept_ThrowsForUnsupportedFormat()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Accept(
            new DocumentId("doc-3"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("TXT"),
            new DocumentMetadata(1024, "text/plain", "en"),
            new Provenance("https://example.com/file.txt", "Example"),
            new CorrelationId("corr-3"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.txt")));

        Assert.Equal("Unsupported format", ex.Message);
    }

    [Fact]
    public void Accept_ThrowsForMissingRequiredMetadata()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Accept(
            new DocumentId("doc-4"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(0, string.Empty, "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-4"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf")));

        Assert.Equal("Validation failure", ex.Message);
    }

    [Fact]
    public void Accept_ThrowsForUnsupportedSource()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Accept(
            new DocumentId("doc-5"),
            new TenantId("tenant-1"),
            new DocumentSource("Email"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(1024, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-5"),
            new IdempotencyKey("tenant-1|email|https://example.com/file.pdf")));

        Assert.Equal("Unsupported source", ex.Message);
    }

    [Fact]
    public void Accept_ThrowsForMissingProvenanceReference()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Accept(
            new DocumentId("doc-6"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(1024, "application/pdf", "en"),
            new Provenance(string.Empty, "Example"),
            new CorrelationId("corr-6"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf")));

        Assert.Equal("Validation failure", ex.Message);
    }

    [Fact]
    public void Accept_ThrowsForEmptyIdempotencyKey()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Accept(
            new DocumentId("doc-7"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(1024, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-7"),
            new IdempotencyKey(string.Empty)));

        Assert.Equal("Validation failure", ex.Message);
    }

    [Fact]
    public void Accept_ThrowsForMissingTenantContext()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Accept(
            new DocumentId("doc-8"),
            new TenantId("   "),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(1024, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-8"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf")));

        Assert.Equal("Invalid tenant context", ex.Message);
    }

    [Fact]
    public void Reject_ThrowsForMissingTenantContext()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Reject(
            new DocumentId("doc-9"),
            new TenantId("   "),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(1024, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-9"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"),
            new RejectionReason("Unsupported format")));

        Assert.Equal("Invalid tenant context", ex.Message);
    }
}
