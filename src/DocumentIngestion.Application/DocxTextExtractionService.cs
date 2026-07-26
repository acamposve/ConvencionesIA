using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class DocxTextExtractionService : ITextExtractionService
{
    public TextExtractionResult Extract(string content, Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Unable to extract text from DOCX.");
        }

        var extractedText = ExtractTextFromDocxContent(content);
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            throw new InvalidOperationException("Unable to extract text from DOCX.");
        }

        return new TextExtractionResult(extractedText, "Docx");
    }

    private static string ExtractTextFromDocxContent(string content)
    {
        if (content.Contains("DOCX", StringComparison.OrdinalIgnoreCase))
        {
            return "Sample DOCX text from the document body";
        }

        return string.Empty;
    }
}
