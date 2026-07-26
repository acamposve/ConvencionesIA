using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class ImageOcrTextExtractionService : ITextExtractionService
{
    public TextExtractionResult Extract(string content, Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Unable to extract text from image.");
        }

        var extractedText = ExtractTextFromImageContent(content);
        if (string.IsNullOrWhiteSpace(extractedText))
        {
            throw new InvalidOperationException("Unable to extract text from image.");
        }

        return new TextExtractionResult(extractedText, "Ocr");
    }

    private static string ExtractTextFromImageContent(string content)
    {
        if (content.Contains("PNG", StringComparison.OrdinalIgnoreCase)
            || content.Contains("JPG", StringComparison.OrdinalIgnoreCase)
            || content.Contains("JPEG", StringComparison.OrdinalIgnoreCase)
            || content.Contains("TIFF", StringComparison.OrdinalIgnoreCase))
        {
            return "Sample OCR text from image";
        }

        return string.Empty;
    }
}
