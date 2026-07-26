using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed record TextExtractionResult(string ExtractedText, string ExtractionStrategy);
