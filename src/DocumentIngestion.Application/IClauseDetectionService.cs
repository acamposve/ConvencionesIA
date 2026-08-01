namespace DocumentIngestion.Application;

public interface IClauseDetectionService
{
    ClauseDetectionResult Detect(string normalizedText);
}

public sealed record ClauseDetectionResult(IReadOnlyList<DetectedClause> Clauses);

public sealed record DetectedClause(
    int Sequence,
    string? NumberLabel,
    string Text,
    int SpanStart,
    int SpanEnd);
