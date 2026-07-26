using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class ExtractTextUseCaseObservabilityTests
{
    [Fact]
    public void Execute_LogsExtractionOutcomeWithRequiredFields()
    {
        var extractionService = new TestTextExtractionService("hello", "Pdf");
        var messages = new List<string>();
        var useCase = new ExtractTextUseCase(extractionService, messages.Add);

        var document = Document.Accept(
            new DocumentId("doc-60"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-60"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        useCase.Execute(document);

        Assert.Contains(messages, message => message.Contains("TextExtraction|DocumentId=doc-60"));
        Assert.Contains(messages, message => message.Contains("TenantId=tenant-1"));
        Assert.Contains(messages, message => message.Contains("ExtractionStrategy=Pdf"));
        Assert.Contains(messages, message => message.Contains("TextLength=5"));
        Assert.Contains(messages, message => message.Contains("CorrelationId=corr-60"));
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
}
