using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class ExtractTextUseCase
{
    private readonly TextExtractionServiceRouter _router;
    private readonly Action<string>? _logger;
    private readonly IIngestionEventPublisher _eventPublisher;

    public ExtractTextUseCase(ITextExtractionService textExtractionService)
        : this(new TextExtractionServiceRouter(textExtractionService, textExtractionService, textExtractionService), null, null)
    {
    }

    public ExtractTextUseCase(ITextExtractionService textExtractionService, Action<string>? logger)
        : this(new TextExtractionServiceRouter(textExtractionService, textExtractionService, textExtractionService), logger, null)
    {
    }

    public ExtractTextUseCase(TextExtractionServiceRouter router, Action<string>? logger)
        : this(router, logger, null)
    {
    }

    public ExtractTextUseCase(TextExtractionServiceRouter router, Action<string>? logger, IIngestionEventPublisher? eventPublisher)
    {
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _logger = logger;
        _eventPublisher = eventPublisher ?? new NullIngestionEventPublisher();
    }

    public Document Execute(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.State == IngestionState.Rejected || document.State == IngestionState.Failed)
        {
            _logger?.Invoke($"TextExtraction|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DocumentType={document.DetectedDocumentType?.Value ?? "Unknown"}|ExtractionStrategy=Skipped|ProcessingTimeMs=0|TextLength=0|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        if (document.HasExtractedText)
        {
            _logger?.Invoke($"TextExtraction|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DocumentType={document.DetectedDocumentType?.Value ?? "Unknown"}|ExtractionStrategy=Existing|ProcessingTimeMs=0|TextLength={document.ExtractedText?.Value.Length ?? 0}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            var extractionContent = GetExtractionContent(document);
            var result = _router.Extract(extractionContent, document);
            document.RecordExtractedText(new RawText(result.ExtractedText));
            _eventPublisher.PublishTextExtracted(document, result.ExtractionStrategy, result.ExtractedText.Length);
            _logger?.Invoke($"TextExtraction|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DocumentType={document.DetectedDocumentType?.Value ?? "Unknown"}|ExtractionStrategy={result.ExtractionStrategy}|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|TextLength={result.ExtractedText.Length}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }
        catch (Exception ex)
        {
            var failureReason = "Extraction failed";
            document.FailExtraction(failureReason);
            _eventPublisher.PublishTextExtractionFailed(document, failureReason);
            _logger?.Invoke($"TextExtraction|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DocumentType={document.DetectedDocumentType?.Value ?? "Unknown"}|ExtractionStrategy=Failed|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|TextLength=0|CorrelationId={document.CorrelationId.Value}");
            _logger?.Invoke($"TextExtractionFailure|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|ErrorType={ex.GetType().Name}|CorrelationId={document.CorrelationId.Value}");
            throw new InvalidOperationException(failureReason, ex);
        }
    }

    private static string GetExtractionContent(Document document)
    {
        var detectedType = document.DetectedDocumentType?.Value ?? document.Metadata.MimeType;
        return detectedType switch
        {
            null or "" => document.Metadata.MimeType,
            "Pdf" or "pdf" or "application/pdf" => "PDF",
            "Docx" or "docx" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "DOCX",
            "Png" or "png" or "image/png" => "PNG",
            "Jpeg" or "jpeg" or "jpg" or "image/jpeg" or "image/jpg" => "JPEG",
            "Tiff" or "tiff" or "image/tiff" => "TIFF",
            _ => detectedType
        };
    }

    private sealed class NullIngestionEventPublisher : IIngestionEventPublisher
    {
        public void Publish(Document document)
        {
        }

        public void PublishTextExtracted(Document document, string extractionStrategy, int textLength)
        {
        }

        public void PublishTextExtractionFailed(Document document, string reason)
        {
        }
    }
}
