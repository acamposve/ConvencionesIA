using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class ExtractTextUseCase
{
    private readonly TextExtractionServiceRouter _router;
    private readonly Action<string>? _logger;
    private readonly IIngestionEventPublisher _eventPublisher;
    private readonly NormalizeTextUseCase? _normalizationUseCase;
    private readonly DetectClausesUseCase? _clauseDetectionUseCase;
    private readonly CategorizeClausesUseCase? _clauseCategorizationUseCase;

    public ExtractTextUseCase(ITextExtractionService textExtractionService)
        : this(new TextExtractionServiceRouter(textExtractionService, textExtractionService, textExtractionService), null, null, null, null, null)
    {
    }

    public ExtractTextUseCase(ITextExtractionService textExtractionService, Action<string>? logger)
        : this(new TextExtractionServiceRouter(textExtractionService, textExtractionService, textExtractionService), logger, null, null, null, null)
    {
    }

    public ExtractTextUseCase(TextExtractionServiceRouter router, Action<string>? logger)
        : this(router, logger, null, null, null, null)
    {
    }

    public ExtractTextUseCase(TextExtractionServiceRouter router, Action<string>? logger, IIngestionEventPublisher? eventPublisher)
        : this(router, logger, eventPublisher, null, null, null)
    {
    }

    public ExtractTextUseCase(TextExtractionServiceRouter router, Action<string>? logger, IIngestionEventPublisher? eventPublisher, NormalizeTextUseCase? normalizationUseCase)
        : this(router, logger, eventPublisher, normalizationUseCase, null, null)
    {
    }

    public ExtractTextUseCase(TextExtractionServiceRouter router, Action<string>? logger, IIngestionEventPublisher? eventPublisher, NormalizeTextUseCase? normalizationUseCase, DetectClausesUseCase? clauseDetectionUseCase)
        : this(router, logger, eventPublisher, normalizationUseCase, clauseDetectionUseCase, null)
    {
    }

    public ExtractTextUseCase(TextExtractionServiceRouter router, Action<string>? logger, IIngestionEventPublisher? eventPublisher, NormalizeTextUseCase? normalizationUseCase, DetectClausesUseCase? clauseDetectionUseCase, CategorizeClausesUseCase? clauseCategorizationUseCase)
    {
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _logger = logger;
        _eventPublisher = eventPublisher ?? new NullIngestionEventPublisher();
        _normalizationUseCase = normalizationUseCase;
        _clauseDetectionUseCase = clauseDetectionUseCase;
        _clauseCategorizationUseCase = clauseCategorizationUseCase;
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

            if (_normalizationUseCase is not null)
            {
                _normalizationUseCase.Execute(document);
            }

            if (_clauseDetectionUseCase is not null && document.HasNormalizedText)
            {
                _clauseDetectionUseCase.Execute(document);
            }

            if (_clauseCategorizationUseCase is not null && document.HasClauses)
            {
                _clauseCategorizationUseCase.Execute(document);
            }

            return document;
        }
        catch (Exception) when (document.State == IngestionState.Failed)
        {
            throw;
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

        public void PublishTextNormalized(Document document, string normalizationStrategy, int textLength)
        {
        }

        public void PublishTextNormalizationFailed(Document document, string reason)
        {
        }

        public void PublishClauseDetectionCompleted(Document document, int clauseCount)
        {
        }

        public void PublishClauseDetectionFailed(Document document, string reason)
        {
        }

        public void PublishClauseCategorizationCompleted(Document document, int clauseCount)
        {
        }

        public void PublishClauseCategorizationFailed(Document document, string reason)
        {
        }
    }
}
