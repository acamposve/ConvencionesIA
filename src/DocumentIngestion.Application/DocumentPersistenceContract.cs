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
    string? Outcome,
    string? RejectionReason,
    string ProcessingStage,
    string State,
    string? DetectedDocumentType,
    string? ExtractedText,
    string? NormalizedText,
    IReadOnlyList<DocumentRevisionPersistenceContract> Revisions,
    IReadOnlyList<ClausePersistenceContract>? Clauses = null,
    IReadOnlyList<ClauseCategoryAssignmentPersistenceContract>? CategoryAssignments = null,
    IReadOnlyList<DocumentClassificationPersistenceContract>? DocumentClassifications = null,
    IReadOnlyList<DocumentSummaryPersistenceContract>? DocumentSummaries = null);

public sealed record DocumentRevisionPersistenceContract(
    int Version,
    DateTimeOffset Timestamp,
    string Outcome,
    string ProcessingStage);

public sealed record ClausePersistenceContract(
    string Id,
    int Sequence,
    string? NumberLabel,
    string Text,
    int SpanStart,
    int SpanEnd);

public sealed record ClauseCategoryAssignmentPersistenceContract(
    string ClauseId,
    string CategoryCode,
    decimal ConfidenceScore);

public sealed record DocumentClassificationPersistenceContract(
    string ClassificationCode,
    decimal ConfidenceScore);

public sealed record DocumentSummaryPersistenceContract(
    string SummaryText);
