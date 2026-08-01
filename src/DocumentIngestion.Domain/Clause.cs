namespace DocumentIngestion.Domain;

public sealed class Clause
{
    private Clause(
        ClauseId id,
        int sequence,
        ClauseText text,
        ClauseSpan span,
        ClauseNumberLabel? numberLabel)
    {
        Id = id;
        Sequence = sequence;
        Text = text;
        Span = span;
        NumberLabel = numberLabel;
    }

    public ClauseId Id { get; }
    public int Sequence { get; }
    public ClauseText Text { get; }
    public ClauseSpan Span { get; }
    public ClauseNumberLabel? NumberLabel { get; }

    public static Clause Create(
        ClauseId id,
        int sequence,
        ClauseText text,
        ClauseSpan span,
        ClauseNumberLabel? numberLabel = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(span);

        if (sequence <= 0)
        {
            throw new DomainValidationException("Clause sequence must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(text.Value))
        {
            throw new DomainValidationException("Clause text is required.");
        }

        if (span.Start < 0 || span.End <= span.Start)
        {
            throw new DomainValidationException("Clause span is invalid.");
        }

        return new Clause(id, sequence, text, span, numberLabel);
    }
}

public sealed record ClauseId(string Value)
{
    public static ClauseId CreateDeterministic(string documentId, int revisionNumber, int sequence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        if (revisionNumber <= 0 || sequence <= 0)
        {
            throw new DomainValidationException("Clause identity components must be positive.");
        }

        return new ClauseId($"{documentId}:{revisionNumber}:{sequence}");
    }

    public override string ToString() => Value;
}

public sealed record ClauseText(string Value)
{
    public override string ToString() => Value;
}

public sealed record ClauseSpan(int Start, int End)
{
    public override string ToString() => $"{Start}:{End}";
}

public sealed record ClauseNumberLabel(string Value)
{
    public override string ToString() => Value;
}
