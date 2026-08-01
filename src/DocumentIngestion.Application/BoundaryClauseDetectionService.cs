using System.Text.RegularExpressions;

namespace DocumentIngestion.Application;

public sealed class BoundaryClauseDetectionService : IClauseDetectionService
{
    private static readonly Regex NumberedClausePattern = new(
        @"(?<!\S)(?<label>(?:[IVXLC]+|[A-Za-z]|\d+)(?:\.\d+)?)(?<separator>[.)])\s+",
        RegexOptions.Compiled);

    public ClauseDetectionResult Detect(string normalizedText)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return new ClauseDetectionResult(Array.Empty<DetectedClause>());
        }

        var clauses = SplitClauses(normalizedText.Trim());
        return new ClauseDetectionResult(clauses);
    }

    private static IReadOnlyList<DetectedClause> SplitClauses(string normalizedText)
    {
        var matches = NumberedClausePattern.Matches(normalizedText);
        if (matches.Count == 0)
        {
            return new[]
            {
                new DetectedClause(1, null, NormalizeClauseText(normalizedText), 0, normalizedText.Length)
            };
        }

        var clauses = new List<DetectedClause>(matches.Count);
        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var contentStart = match.Index + match.Length;
            var contentEnd = index + 1 < matches.Count ? matches[index + 1].Index : normalizedText.Length;
            var contentText = normalizedText[contentStart..contentEnd].Trim();
            var clauseText = NormalizeClauseText(contentText);

            if (string.IsNullOrWhiteSpace(clauseText))
            {
                continue;
            }

            clauses.Add(new DetectedClause(
                clauses.Count + 1,
                match.Groups["label"].Value,
                clauseText,
                contentStart,
                contentStart + clauseText.Length));
        }

        return clauses;
    }

    private static string NormalizeClauseText(string text)
    {
        return text.Trim().TrimEnd('.', ';', ':', ')', ']', '"', '\'').Trim();
    }
}
