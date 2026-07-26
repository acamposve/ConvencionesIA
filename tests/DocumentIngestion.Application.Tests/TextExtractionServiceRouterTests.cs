using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class TextExtractionServiceRouterTests
{
    [Fact]
    public void Extract_UsesPdfStrategyForPdfDocument()
    {
        var router = new TextExtractionServiceRouter(
            new PdfTextExtractionService(),
            new DocxTextExtractionService(),
            new ImageOcrTextExtractionService());

        var document = Document.Accept(
            new DocumentId("doc-60"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-60"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));
        document.RecordDetectedDocumentType(new DocumentType("Pdf"));

        var result = router.Extract("PDF", document);

        Assert.Equal("Pdf", result.ExtractionStrategy);
        Assert.Equal("Sample PDF text from page 1\nSample PDF text from page 2", result.ExtractedText);
    }

    [Fact]
    public void Extract_UsesDocxStrategyForDocxDocument()
    {
        var router = new TextExtractionServiceRouter(
            new PdfTextExtractionService(),
            new DocxTextExtractionService(),
            new ImageOcrTextExtractionService());

        var document = Document.Accept(
            new DocumentId("doc-61"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("Word"),
            new DocumentMetadata(2048, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "en"),
            new Provenance("https://example.com/file.docx", "Example"),
            new CorrelationId("corr-61"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.docx"));
        document.RecordDetectedDocumentType(new DocumentType("Docx"));

        var result = router.Extract("DOCX", document);

        Assert.Equal("Docx", result.ExtractionStrategy);
        Assert.Equal("Sample DOCX text from the document body", result.ExtractedText);
    }

    [Fact]
    public void Extract_UsesImageStrategyForImageMimeType()
    {
        var router = new TextExtractionServiceRouter(
            new PdfTextExtractionService(),
            new DocxTextExtractionService(),
            new ImageOcrTextExtractionService());

        var document = Document.Accept(
            new DocumentId("doc-62"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("Image"),
            new DocumentMetadata(2048, "image/png", "en"),
            new Provenance("https://example.com/file.png", "Example"),
            new CorrelationId("corr-62"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.png"));

        var result = router.Extract("PNG", document);

        Assert.Equal("Ocr", result.ExtractionStrategy);
        Assert.Equal("Sample OCR text from image", result.ExtractedText);
    }

    [Fact]
    public void Extract_ThrowsForUnsupportedDocumentType()
    {
        var router = new TextExtractionServiceRouter(
            new PdfTextExtractionService(),
            new DocxTextExtractionService(),
            new ImageOcrTextExtractionService());

        var document = Document.Accept(
            new DocumentId("doc-63"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-63"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));
        document.RecordDetectedDocumentType(new DocumentType("Unknown"));

        var ex = Assert.Throws<InvalidOperationException>(() => router.Extract("PDF", document));

        Assert.Equal("Unsupported document type for text extraction.", ex.Message);
    }
}
