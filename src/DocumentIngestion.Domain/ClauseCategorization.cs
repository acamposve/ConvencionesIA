namespace DocumentIngestion.Domain;

public sealed class ClauseCategoryAssignment
{
    private ClauseCategoryAssignment(
        ClauseId clauseId,
        ClauseCategoryCode categoryCode,
        ConfidenceScore confidenceScore)
    {
        ClauseId = clauseId;
        CategoryCode = categoryCode;
        ConfidenceScore = confidenceScore;
    }

    public ClauseId ClauseId { get; }
    public ClauseCategoryCode CategoryCode { get; }
    public ConfidenceScore ConfidenceScore { get; }

    public static ClauseCategoryAssignment Create(
        ClauseId clauseId,
        ClauseCategoryCode categoryCode,
        ConfidenceScore confidenceScore)
    {
        ArgumentNullException.ThrowIfNull(clauseId);
        ArgumentNullException.ThrowIfNull(categoryCode);
        ArgumentNullException.ThrowIfNull(confidenceScore);

        if (string.IsNullOrWhiteSpace(clauseId.Value))
        {
            throw new DomainValidationException("Clause identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(categoryCode.Value))
        {
            throw new DomainValidationException("Category code is required.");
        }

        if (confidenceScore.Value < 0m || confidenceScore.Value > 1m)
        {
            throw new DomainValidationException("Confidence score must be between 0.0 and 1.0.");
        }

        return new ClauseCategoryAssignment(clauseId, categoryCode, confidenceScore);
    }
}

public sealed record ClauseCategoryCode(string Value)
{
    public override string ToString() => Value;
}

public sealed record ConfidenceScore(decimal Value)
{
    public decimal NormalizedValue => Math.Clamp(Value, 0m, 1m);

    public override string ToString() => Value.ToString("0.00");
}
