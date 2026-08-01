namespace DocumentIngestion.Domain;

public sealed class DocumentClassificationResult
{
    private DocumentClassificationResult(
        DocumentClassificationCode classificationCode,
        ConfidenceScore confidenceScore)
    {
        ClassificationCode = classificationCode;
        ConfidenceScore = confidenceScore;
    }

    public DocumentClassificationCode ClassificationCode { get; }
    public ConfidenceScore ConfidenceScore { get; }

    public static DocumentClassificationResult Create(
        DocumentClassificationCode classificationCode,
        ConfidenceScore confidenceScore)
    {
        ArgumentNullException.ThrowIfNull(classificationCode);
        ArgumentNullException.ThrowIfNull(confidenceScore);

        if (string.IsNullOrWhiteSpace(classificationCode.Value))
        {
            throw new DomainValidationException("Classification code is required.");
        }

        if (confidenceScore.Value < 0m || confidenceScore.Value > 1m)
        {
            throw new DomainValidationException("Confidence score must be between 0.0 and 1.0.");
        }

        return new DocumentClassificationResult(classificationCode, confidenceScore);
    }
}

public sealed record DocumentClassificationCode(string Value)
{
    public override string ToString() => Value;
}
