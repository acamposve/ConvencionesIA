namespace DocumentIngestion.Application;

public sealed record DocumentPersistenceContract(
    string Id,
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
    string? SourceName,
    string CorrelationId,
    string IdempotencyKey,
    string Outcome,
    string? RejectionReason,
    string ProcessingStage,
    string State,
    IReadOnlyList<DocumentRevisionPersistenceContract> Revisions);

public sealed record DocumentRevisionPersistenceContract(
    int Version,
    DateTimeOffset Timestamp,
    string Outcome,
    string ProcessingStage);
