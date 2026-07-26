using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public interface IDocumentTypeDetectionService
{
    DocumentType Detect(string mimeType);
}
