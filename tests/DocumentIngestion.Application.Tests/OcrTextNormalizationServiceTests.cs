using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class OcrTextNormalizationServiceTests
{
    [Fact]
    public void Normalize_ReducesWhitespaceAndLineBreakArtifacts()
    {
        var service = new OcrTextNormalizationService();
        var document = CreateDocument();

        var result = service.Normalize("Hello   \t\tworld\r\n\r\nNext\nline", document);

        Assert.Equal("Hello world\nNext line", result.NormalizedText);
        Assert.Equal("OcrCleanup", result.NormalizationStrategy);
    }

    [Fact]
    public void Normalize_RemovesNonPrintableCharactersAndNormalizesQuotes()
    {
        var service = new OcrTextNormalizationService();
        var document = CreateDocument();

        var result = service.Normalize("\u0000“Hello”\u0007\n‘world’", document);

        Assert.Equal("\"Hello\"\n'world'", result.NormalizedText);
    }

    [Fact]
    public void Normalize_ThrowsForEmptyContent()
    {
        var service = new OcrTextNormalizationService();
        var document = CreateDocument();

        Assert.Throws<InvalidOperationException>(() => service.Normalize(string.Empty, document));
    }

    private static Document CreateDocument()
    {
        return Document.Accept(
            new DocumentId("doc-1"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(1024, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-1"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));
    }
}
