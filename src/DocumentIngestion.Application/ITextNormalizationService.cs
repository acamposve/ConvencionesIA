using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public interface ITextNormalizationService
{
    TextNormalizationResult Normalize(string content, Document document);
}
