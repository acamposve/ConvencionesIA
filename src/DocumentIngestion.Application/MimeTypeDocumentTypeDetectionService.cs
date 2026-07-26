using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class MimeTypeDocumentTypeDetectionService : IDocumentTypeDetectionService
{
    public DocumentType Detect(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            return new DocumentType("Unknown");
        }

        return mimeType.ToLowerInvariant() switch
        {
            "application/pdf" => new DocumentType("Pdf"),
            "application/msword" => new DocumentType("Doc"),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => new DocumentType("Docx"),
            "image/png" => new DocumentType("Png"),
            "image/jpeg" => new DocumentType("Jpeg"),
            "image/tiff" => new DocumentType("Tiff"),
            _ => new DocumentType("Unknown")
        };
    }
}
