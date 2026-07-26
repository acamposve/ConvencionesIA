using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocxTextExtractionServiceTests
{
    [Fact]
    public void Extract_ReturnsSampleTextForDocxContent()
    {
        var service = new DocxTextExtractionService();
        var document = Document.Accept(
            new DocumentId("doc-40"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "en"),
            new Provenance("https://example.com/file.docx", "Example"),
            new CorrelationId("corr-40"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.docx"));

        var result = service.Extract("DOCX", document);

        Assert.Equal("Sample DOCX text from the document body", result.ExtractedText);
        Assert.Equal("Docx", result.ExtractionStrategy);
    }

    [Fact]
    public void Extract_ThrowsWhenDocxContentIsEmpty()
    {
        var service = new DocxTextExtractionService();
        var document = Document.Accept(
            new DocumentId("doc-41"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "en"),
            new Provenance("https://example.com/file.docx", "Example"),
            new CorrelationId("corr-41"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.docx"));

        var ex = Assert.Throws<InvalidOperationException>(() => service.Extract(string.Empty, document));

        Assert.Equal("Unable to extract text from DOCX.", ex.Message);
    }
}
