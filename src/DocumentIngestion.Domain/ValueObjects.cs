namespace DocumentIngestion.Domain;

public sealed record DocumentId(string Value)
{
    public override string ToString() => Value;
}

public sealed record TenantId(string Value)
{
    public override string ToString() => Value;
}

public sealed record DocumentSource(string Value)
{
    public override string ToString() => Value;
}

public sealed record DocumentFormat(string Value)
{
    public override string ToString() => Value;
}

public sealed record DocumentType
{
    private static readonly HashSet<string> SupportedValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pdf",
        "Doc",
        "Docx",
        "Png",
        "Jpeg",
        "Tiff",
        "Unknown"
    };

    public DocumentType(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var trimmedValue = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmedValue))
        {
            throw new DomainValidationException("Unsupported document type");
        }

        var normalizedValue = Normalize(trimmedValue);
        if (!SupportedValues.Contains(normalizedValue))
        {
            throw new DomainValidationException("Unsupported document type");
        }

        Value = normalizedValue;
    }

    public string Value { get; }

    public override string ToString() => Value;

    private static string Normalize(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "pdf" => "Pdf",
            "doc" => "Doc",
            "docx" => "Docx",
            "png" => "Png",
            "jpg" or "jpeg" => "Jpeg",
            "tiff" => "Tiff",
            "unknown" => "Unknown",
            _ => value.Trim()
        };
    }
}

public sealed record CorrelationId(string Value)
{
    public override string ToString() => Value;
}

public sealed record IdempotencyKey(string Value)
{
    public override string ToString() => Value;
}

public sealed record Provenance(string SourceReference, string? SourceName = null)
{
    public override string ToString() => SourceReference;
}

public sealed record DocumentMetadata(
    long FileSizeBytes,
    string MimeType,
    string? Language = null,
    int? PageCount = null,
    string? Author = null,
    DateTimeOffset? CreationDate = null)
{
    public bool HasRequiredMetadata => FileSizeBytes > 0 && !string.IsNullOrWhiteSpace(MimeType);
}

public sealed record DocumentRevision(int Version, DateTimeOffset Timestamp, IngestionOutcome Outcome, ProcessingStage ProcessingStage);

public enum ProcessingStage
{
    None,
    PendingProcessing,
    ClausesDetected,
    ClausesCategorized,
    DocumentClassified
}

public enum IngestionState
{
    PendingAcceptance,
    Accepted,
    Rejected,
    Failed
}

public enum IngestionOutcome
{
    Accepted,
    Rejected,
    Failed
}

public sealed record RejectionReason(string Value)
{
    public override string ToString() => Value;
}

public sealed record RawText(string Value)
{
    public override string ToString() => Value;
}

public sealed record NormalizedText(string Value)
{
    public override string ToString() => Value;
}
