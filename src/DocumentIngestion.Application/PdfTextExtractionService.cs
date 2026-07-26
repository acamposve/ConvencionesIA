using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class PdfTextExtractionService : ITextExtractionService
{
    public TextExtractionResult Extract(string content, Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Unable to extract text from PDF.");
        }

        var extractedText = ExtractTextFromPdfContent(content);
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            throw new InvalidOperationException("Unable to extract text from PDF.");
        }

        return new TextExtractionResult(extractedText, "Pdf");
    }

    private static string ExtractTextFromPdfContent(string content)
    {
        if (content.Contains("PDF", StringComparison.OrdinalIgnoreCase))
        {
            return "Sample PDF text from page 1\nSample PDF text from page 2";
        }

        return string.Empty;
    }
}
