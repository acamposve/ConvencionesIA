using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class ExtractTextUseCaseTests
{
    [Fact]
    public void Execute_RecordsExtractedTextWhenExtractionSucceeds()
    {
        var extractionService = new TestTextExtractionService("hello world", "Pdf");
        var useCase = new ExtractTextUseCase(extractionService);

        var document = Document.Accept(
            new DocumentId("doc-20"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-20"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.True(result.HasExtractedText);
        Assert.Equal("hello world", result.ExtractedText?.Value);
    }

    [Fact]
    public void Execute_FailsDocumentWhenExtractionThrows()
    {
        var extractionService = new ThrowingTextExtractionService();
        var useCase = new ExtractTextUseCase(extractionService);

        var document = Document.Accept(
            new DocumentId("doc-21"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-21"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("boom", ex.Message);
        Assert.Equal(IngestionState.Rejected, document.State);
        Assert.Equal("boom", document.RejectionReason?.Value);
    }

    [Fact]
    public void Execute_FailsDocumentWhenExtractionThrowsUnexpectedException()
    {
        var extractionService = new UnexpectedExceptionTextExtractionService();
        var useCase = new ExtractTextUseCase(extractionService);

        var document = Document.Accept(
            new DocumentId("doc-22"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-22"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<ArgumentException>(() => useCase.Execute(document));

        Assert.Equal("unexpected", ex.Message);
        Assert.Equal(IngestionState.Rejected, document.State);
        Assert.Equal("unexpected", document.RejectionReason?.Value);
    }

    [Fact]
    public void Execute_DoesNotReprocessDocumentThatAlreadyHasExtractedText()
    {
        var extractionService = new TestTextExtractionService("new text", "Pdf");
        var useCase = new ExtractTextUseCase(extractionService);

        var document = Document.Accept(
            new DocumentId("doc-23"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-23"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));
        document.RecordExtractedText(new RawText("original text"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.Equal("original text", result.ExtractedText?.Value);
    }

    private sealed class TestTextExtractionService : ITextExtractionService
    {
        private readonly string _text;
        private readonly string _strategy;

        public TestTextExtractionService(string text, string strategy)
        {
            _text = text;
            _strategy = strategy;
        }

        public TextExtractionResult Extract(string content, Document document)
        {
            return new TextExtractionResult(_text, _strategy);
        }
    }

    private sealed class ThrowingTextExtractionService : ITextExtractionService
    {
        public TextExtractionResult Extract(string content, Document document)
        {
            throw new InvalidOperationException("boom");
        }
    }

    private sealed class UnexpectedExceptionTextExtractionService : ITextExtractionService
    {
        public TextExtractionResult Extract(string content, Document document)
        {
            throw new ArgumentException("unexpected");
        }
    }
}
