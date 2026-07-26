using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class ExtractTextUseCase
{
    private readonly TextExtractionServiceRouter _router;
    private readonly Action<string>? _logger;
    private readonly DocumentIngestionEventPublisher? _eventPublisher;

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

    public ExtractTextUseCase(TextExtractionServiceRouter router, Action<string>? logger, DocumentIngestionEventPublisher? eventPublisher)
    {
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public Document Execute(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.HasExtractedText)
        {
            _logger?.Invoke($"TextExtraction|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DocumentType={document.DetectedDocumentType?.Value ?? "Unknown"}|ExtractionStrategy=Existing|ProcessingTimeMs=0|TextLength={document.ExtractedText?.Value.Length ?? 0}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }

        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            var result = _router.Extract(document.Metadata.MimeType, document);
            document.RecordExtractedText(new RawText(result.ExtractedText));
            _eventPublisher?.PublishTextExtracted(document, result.ExtractionStrategy, result.ExtractedText.Length);
            _logger?.Invoke($"TextExtraction|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DocumentType={document.DetectedDocumentType?.Value ?? "Unknown"}|ExtractionStrategy={result.ExtractionStrategy}|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|TextLength={result.ExtractedText.Length}|CorrelationId={document.CorrelationId.Value}");
            return document;
        }
        catch (Exception ex)
        {
            document.FailExtraction(ex.Message);
            _eventPublisher?.PublishTextExtractionFailed(document, ex.Message);
            _logger?.Invoke($"TextExtraction|DocumentId={document.Id.Value}|TenantId={document.TenantId.Value}|DocumentType={document.DetectedDocumentType?.Value ?? "Unknown"}|ExtractionStrategy=Failed|ProcessingTimeMs={(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds:0}|TextLength=0|CorrelationId={document.CorrelationId.Value}");
            throw;
        }
    }
}
