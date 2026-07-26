using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class ImageOcrTextExtractionServiceTests
{
    [Fact]
    public void Extract_ReturnsSampleTextForImageContent()
    {
        var service = new ImageOcrTextExtractionService();
        var document = Document.Accept(
            new DocumentId("doc-50"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("Image"),
            new DocumentMetadata(2048, "image/png", "en"),
            new Provenance("https://example.com/file.png", "Example"),
            new CorrelationId("corr-50"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.png"));

        var result = service.Extract("PNG", document);

        Assert.Equal("Sample OCR text from image", result.ExtractedText);
        Assert.Equal("Ocr", result.ExtractionStrategy);
    }

    [Fact]
    public void Extract_ThrowsWhenImageContentIsEmpty()
    {
        var service = new ImageOcrTextExtractionService();
        var document = Document.Accept(
            new DocumentId("doc-51"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("Image"),
            new DocumentMetadata(2048, "image/jpeg", "en"),
            new Provenance("https://example.com/file.jpg", "Example"),
            new CorrelationId("corr-51"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.jpg"));

        var ex = Assert.Throws<InvalidOperationException>(() => service.Extract(string.Empty, document));

        Assert.Equal("Unable to extract text from image.", ex.Message);
    }
}
