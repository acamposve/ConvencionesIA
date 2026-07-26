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
        _detectionService = detectionService;
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
                _logger?.Invoke($"DocumentTypeDetection|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DetectedType=Unknown|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
                throw new DomainValidationException("Unsupported document type");
            }

            document.RecordDetectedDocumentType(detectedType);
            _logger?.Invoke($"DocumentTypeDetection|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DetectedType={detectedType.Value}|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }
        catch (DomainValidationException)
        {
            _logger?.Invoke($"DocumentTypeDetection|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DetectedType=Unknown|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|CorrelationId={document.CorrelationId.Value}");
            throw;
        }
    }
}
