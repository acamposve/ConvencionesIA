using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class TextExtractionServiceRouter
{
    private readonly ITextExtractionService _pdfService;
    private readonly ITextExtractionService _docxService;
    private readonly ITextExtractionService _imageService;

    public TextExtractionServiceRouter(
        ITextExtractionService pdfService,
        ITextExtractionService docxService,
        ITextExtractionService imageService)
    {
        _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
        _docxService = docxService ?? throw new ArgumentNullException(nameof(docxService));
        _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
    }

    public TextExtractionResult Extract(string content, Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var documentType = document.DetectedDocumentType?.Value ?? document.Metadata.MimeType;
        return ResolveStrategy(documentType).Extract(content, document);
    }

    private ITextExtractionService ResolveStrategy(string documentType)
    {
        return documentType.ToLowerInvariant() switch
        {
            "pdf" or "application/pdf" => _pdfService,
            "doc" or "application/msword" or "docx" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => _docxService,
            "png" or "image/png" or "jpeg" or "image/jpeg" or "jpg" or "image/jpg" or "tiff" or "image/tiff" => _imageService,
            _ => throw new InvalidOperationException("Unsupported document type for text extraction.")
        };
    }
}
