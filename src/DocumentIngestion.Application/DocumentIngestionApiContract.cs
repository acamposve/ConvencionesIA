namespace DocumentIngestion.Application;

public static class DocumentIngestionApiContract
{
    public const string Route = "/api/v1/documents/ingestion";
    public const string Version = "v1";
    public const string AuthenticationRequirement = "Authentication is required for this operation.";
    public const string AuthorizationRequirement = "Authorization must ensure the caller can ingest documents for the specified tenant.";
}

public sealed record IngestDocumentRequestContract(
    string TenantId,
    string Source,
    string Format,
    long FileSizeBytes,
    string MimeType,
    string? Language,
    int? PageCount,
    string? Author,
    DateTimeOffset? CreationDate,
    string SourceReference,
    string CorrelationId,
    string IdempotencyKey);

public sealed record IngestDocumentResponseContract(
    string DocumentId,
    string Outcome,
    string ProcessingStage,
    string? RejectionReason,
    string CorrelationId,
    DateTimeOffset Timestamp)
{
    public string Version { get; init; } = DocumentIngestionApiContract.Version;
}
