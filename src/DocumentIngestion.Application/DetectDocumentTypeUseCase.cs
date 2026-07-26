using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class DetectDocumentTypeUseCase
{
    private readonly IDocumentTypeDetectionService _detectionService;
    private readonly Action<string>? _logger;

    public DetectDocumentTypeUseCase(IDocumentTypeDetectionService detectionService)
        : this(detectionService, null)
    {
    }

    public DetectDocumentTypeUseCase(IDocumentTypeDetectionService detectionService, Action<string>? logger)
    {
        _detectionService = detectionService ?? throw new ArgumentNullException(nameof(detectionService));
        _logger = logger;
    }

    public Document Execute(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.HasDetectedDocumentType)
        {
            _logger?.Invoke($"DocumentTypeDetection|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DetectedType={document.DetectedDocumentType?.Value ?? "Unknown"}|ProcessingTimeMs=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            var detectedType = _detectionService.Detect(document.Metadata.MimeType);

            if (detectedType.Value.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                RejectDocument(document, "Unsupported document type", startedAt);
                throw new DomainValidationException("Unsupported document type");
            }

            document.RecordDetectedDocumentType(detectedType);
            _logger?.Invoke($"DocumentTypeDetection|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DetectedType={detectedType.Value}|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }
        catch (DomainValidationException ex)
        {
            if (document.State == IngestionState.Accepted)
            {
                RejectDocument(document, ex.Message, startedAt);
            }

            throw;
        }
    }

    private void RejectDocument(Document document, string reason, DateTimeOffset startedAt)
    {
        document.RejectForProcessingFailure(new RejectionReason(reason));
        _logger?.Invoke($"DocumentTypeDetection|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DetectedType=Unknown|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
    }
}
