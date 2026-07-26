using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class PdfTextExtractionServiceTests
{
    [Fact]
    public void Extract_ReturnsSampleTextForPdfContent()
    {
        var service = new PdfTextExtractionService();
        var document = Document.Accept(
            new DocumentId("doc-30"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-30"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = service.Extract("PDF", document);

        Assert.Equal("Sample PDF text from page 1\nSample PDF text from page 2", result.ExtractedText);
        Assert.Equal("Pdf", result.ExtractionStrategy);
    }

    [Fact]
    public void Extract_ThrowsWhenPdfContentIsEmpty()
    {
        var service = new PdfTextExtractionService();
        var document = Document.Accept(
            new DocumentId("doc-31"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-31"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => service.Extract(string.Empty, document));

        Assert.Equal("Unable to extract text from PDF.", ex.Message);
    }
}
