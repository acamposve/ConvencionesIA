using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public interface ITextExtractionService
{
    TextExtractionResult Extract(string content, Document document);
}
