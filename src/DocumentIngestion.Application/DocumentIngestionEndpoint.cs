namespace DocumentIngestion.Application;

public sealed class DocumentIngestionEndpoint
{
    private readonly IngestDocumentUseCase _useCase;
    private readonly TenantSecurityGuard _securityGuard;

    public DocumentIngestionEndpoint(IngestDocumentUseCase useCase)
        : this(useCase, new TenantSecurityGuard())
    {
    }

    public DocumentIngestionEndpoint(IngestDocumentUseCase useCase, TenantSecurityGuard securityGuard)
    {
        _useCase = useCase;
        _securityGuard = securityGuard;
    }

    public IngestDocumentResponseContract Handle(IngestDocumentRequestContract request, string? userId = null, string? callerTenantId = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        Validate(request);

        var securityContext = _securityGuard.Authenticate(userId, callerTenantId ?? request.TenantId);
        _securityGuard.Authorize(securityContext, request.TenantId);

        var useCaseResult = _useCase.Execute(new IngestionRequest(
            request.TenantId,
            request.Source,
            request.Format,
            request.FileSizeBytes,
            request.MimeType,
            request.Language,
            request.PageCount,
            request.Author,
            request.CreationDate,
            request.SourceReference,
            request.CorrelationId,
            request.IdempotencyKey));

        return new IngestDocumentResponseContract(
            useCaseResult.DocumentId,
            useCaseResult.RejectionReason is null ? "Accepted" : "Rejected",
            useCaseResult.ProcessingStage,
            useCaseResult.RejectionReason,
            request.CorrelationId,
            DateTimeOffset.UtcNow);
    }

    private static void Validate(IngestDocumentRequestContract request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            throw new ArgumentException("TenantId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Source))
        {
            throw new ArgumentException("Source is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Format))
        {
            throw new ArgumentException("Format is required.", nameof(request));
        }

        if (request.FileSizeBytes <= 0)
        {
            throw new ArgumentException("FileSizeBytes must be greater than zero.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.MimeType))
        {
            throw new ArgumentException("MimeType is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SourceReference))
        {
            throw new ArgumentException("SourceReference is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            throw new ArgumentException("CorrelationId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ArgumentException("IdempotencyKey is required.", nameof(request));
        }
    }
}
