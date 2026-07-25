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
    PendingProcessing
}

public enum IngestionState
{
    PendingAcceptance,
    Accepted,
    Rejected
}

public enum IngestionOutcome
{
    Accepted,
    Rejected
}

public sealed record RejectionReason(string Value)
{
    public override string ToString() => Value;
}
