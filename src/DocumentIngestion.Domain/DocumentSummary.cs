namespace DocumentIngestion.Domain;

public sealed class DocumentSummaryResult
{
    private DocumentSummaryResult(SummaryText summaryText)
    {
        SummaryText = summaryText;
    }

    public SummaryText SummaryText { get; }

    public static DocumentSummaryResult Create(SummaryText summaryText)
    {
        ArgumentNullException.ThrowIfNull(summaryText);

        if (string.IsNullOrWhiteSpace(summaryText.Value))
        {
            throw new DomainValidationException("Summary text is required.");
        }

        return new DocumentSummaryResult(summaryText);
    }
}

public sealed record SummaryText(string Value)
{
    public override string ToString() => Value;
}
