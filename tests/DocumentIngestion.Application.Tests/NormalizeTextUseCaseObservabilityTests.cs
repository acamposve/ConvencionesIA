using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class NormalizeTextUseCaseObservabilityTests
{
    [Fact]
    public void Execute_LogsStructuredMetricsWithoutExposingContentOnSuccess()
    {
        var messages = new List<string>();
        var normalizationService = new StubNormalizationService("normalized text");
        var useCase = new NormalizeTextUseCase(normalizationService, messages.Add);
        var document = CreateDocument("raw OCR text");

        useCase.Execute(document);

        Assert.Contains(messages, message => message.Contains("TextNormalization|")
            && message.Contains("DocumentId=doc-100")
            && message.Contains("TenantId=tenant-1")
            && message.Contains("OriginalTextLength=")
            && message.Contains("NormalizedTextLength=")
            && message.Contains("CorrelationId=corr-100")
            && !message.Contains("raw OCR text")
            && !message.Contains("normalized text"));
    }

    [Fact]
    public void Execute_LogsFailureWithoutExposingContentOnException()
    {
        var messages = new List<string>();
        var normalizationService = new ThrowingNormalizationService();
        var useCase = new NormalizeTextUseCase(normalizationService, messages.Add);
        var document = CreateDocument("sensitive OCR content");

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Normalization failed", ex.Message);
        Assert.Contains(messages, message => message.Contains("TextNormalizationFailure|")
            && message.Contains("ErrorType=InvalidOperationException")
            && message.Contains("CorrelationId=corr-100")
            && !message.Contains("sensitive OCR content"));
    }

    private static Document CreateDocument(string extractedText)
    {
        var document = Document.Accept(
            new DocumentId("doc-100"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-100"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText(extractedText));
        return document;
    }

    private sealed class StubNormalizationService : ITextNormalizationService
    {
        private readonly string _normalizedText;

        public StubNormalizationService(string normalizedText)
        {
            _normalizedText = normalizedText;
        }

        public TextNormalizationResult Normalize(string content, Document document)
        {
            return new TextNormalizationResult(_normalizedText, "TestNormalization");
        }
    }

    private sealed class ThrowingNormalizationService : ITextNormalizationService
    {
        public TextNormalizationResult Normalize(string content, Document document)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
