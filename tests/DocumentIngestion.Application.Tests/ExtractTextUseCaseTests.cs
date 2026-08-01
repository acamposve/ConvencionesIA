using DocumentIngestion.Domain;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class ExtractTextUseCaseTests
{
    [Fact]
    public void Execute_RecordsExtractedTextWhenExtractionSucceeds()
    {
        var extractionService = new TestTextExtractionService("hello world", "Pdf");
        var useCase = new ExtractTextUseCase(extractionService);

        var document = Document.Accept(
            new DocumentId("doc-20"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-20"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.True(result.HasExtractedText);
        Assert.Equal("hello world", result.ExtractedText?.Value);
    }

    [Fact]
    public void Execute_FailsDocumentWhenExtractionThrows()
    {
        var extractionService = new ThrowingTextExtractionService();
        var useCase = new ExtractTextUseCase(extractionService);

        var document = Document.Accept(
            new DocumentId("doc-21"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-21"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Extraction failed", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal("Extraction failed", document.RejectionReason?.Value);
    }

    [Fact]
    public void Execute_FailsDocumentWhenExtractionThrowsUnexpectedException()
    {
        var extractionService = new UnexpectedExceptionTextExtractionService();
        var useCase = new ExtractTextUseCase(extractionService);

        var document = Document.Accept(
            new DocumentId("doc-22"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-22"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Extraction failed", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal("Extraction failed", document.RejectionReason?.Value);
    }

    [Fact]
    public void Execute_DoesNotReprocessDocumentThatAlreadyHasExtractedText()
    {
        var extractionService = new TestTextExtractionService("new text", "Pdf");
        var useCase = new ExtractTextUseCase(extractionService);

        var document = Document.Accept(
            new DocumentId("doc-23"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-23"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));
        document.RecordExtractedText(new RawText("original text"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.Equal("original text", result.ExtractedText?.Value);
    }

    [Fact]
    public void Execute_NormalizesExtractedTextAfterSuccessfulExtraction()
    {
        var extractionService = new TestTextExtractionService("hello world", "Pdf");
        var normalizationService = new TestTextNormalizationService("normalized hello world");
        var normalizationUseCase = new NormalizeTextUseCase(normalizationService);
        var useCase = new ExtractTextUseCase(
            new TextExtractionServiceRouter(extractionService, extractionService, extractionService),
            null,
            null,
            normalizationUseCase);

        var document = Document.Accept(
            new DocumentId("doc-26"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-26"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.True(result.HasNormalizedText);
        Assert.Equal("normalized hello world", result.NormalizedText?.Value);
    }

    [Fact]
    public void Execute_DoesNotNormalizeWhenTextIsAlreadyNormalized()
    {
        var extractionService = new TestTextExtractionService("hello world", "Pdf");
        var normalizationService = new TestTextNormalizationService("should not be used");
        var normalizationUseCase = new NormalizeTextUseCase(normalizationService);
        var useCase = new ExtractTextUseCase(
            new TextExtractionServiceRouter(extractionService, extractionService, extractionService),
            null,
            null,
            normalizationUseCase);

        var document = Document.Accept(
            new DocumentId("doc-28"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-28"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));
        document.RecordExtractedText(new RawText("existing text"));
        document.RecordNormalizedText(new NormalizedText("existing normalized text"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.Equal("existing normalized text", result.NormalizedText?.Value);
    }

    [Fact]
    public void Execute_DetectsClausesAfterSuccessfulNormalization()
    {
        var extractionService = new TestTextExtractionService("1. First clause. 2. Second clause.", "Pdf");
        var normalizationService = new TestTextNormalizationService("1. First clause. 2. Second clause.");
        var normalizationUseCase = new NormalizeTextUseCase(normalizationService);
        var clauseDetectionUseCase = new DetectClausesUseCase(new BoundaryClauseDetectionService());
        var useCase = new ExtractTextUseCase(
            new TextExtractionServiceRouter(extractionService, extractionService, extractionService),
            null,
            null,
            normalizationUseCase,
            clauseDetectionUseCase);

        var document = Document.Accept(
            new DocumentId("doc-29"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-29"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.True(result.HasClauses);
        Assert.Equal(2, result.Clauses.Count);
        Assert.Equal("First clause", result.Clauses[0].Text.Value);
        Assert.Equal("Second clause", result.Clauses[1].Text.Value);
    }

    [Fact]
    public void Execute_CategorizesDetectedClausesAfterSuccessfulDetection()
    {
        var extractionService = new TestTextExtractionService("1. First clause. 2. Second clause.", "Pdf");
        var normalizationService = new TestTextNormalizationService("1. First clause. 2. Second clause.");
        var normalizationUseCase = new NormalizeTextUseCase(normalizationService);
        var clauseDetectionUseCase = new DetectClausesUseCase(new BoundaryClauseDetectionService());
        var clauseCategorizationUseCase = new CategorizeClausesUseCase(new BoundaryClauseCategorizationService());
        var useCase = new ExtractTextUseCase(
            new TextExtractionServiceRouter(extractionService, extractionService, extractionService),
            null,
            null,
            normalizationUseCase,
            clauseDetectionUseCase,
            clauseCategorizationUseCase);

        var document = Document.Accept(
            new DocumentId("doc-30"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-30"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.True(result.HasCategoryAssignments);
        Assert.Equal(2, result.CategoryAssignments.Count);
        Assert.Equal("Obligation", result.CategoryAssignments[0].CategoryCode.Value);
    }

    [Fact]
    public void Execute_FailsDocumentWhenClauseCategorizationThrows()
    {
        var extractionService = new TestTextExtractionService("1. First clause. 2. Second clause.", "Pdf");
        var normalizationService = new TestTextNormalizationService("1. First clause. 2. Second clause.");
        var normalizationUseCase = new NormalizeTextUseCase(normalizationService);
        var clauseDetectionUseCase = new DetectClausesUseCase(new BoundaryClauseDetectionService());
        var clauseCategorizationUseCase = new CategorizeClausesUseCase(new ThrowingClauseCategorizationService());
        var useCase = new ExtractTextUseCase(
            new TextExtractionServiceRouter(extractionService, extractionService, extractionService),
            null,
            null,
            normalizationUseCase,
            clauseDetectionUseCase,
            clauseCategorizationUseCase);

        var document = Document.Accept(
            new DocumentId("doc-31"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-31"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Clause categorization failed", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(IngestionOutcome.Failed, document.Outcome);
        Assert.Equal("Clause categorization failed", document.RejectionReason?.Value);
    }

    [Fact]
    public void Execute_UsesDetectedDocumentTypeForDocxExtraction()
    {
        var useCase = new ExtractTextUseCase(new DocxTextExtractionService());

        var document = Document.Accept(
            new DocumentId("doc-24"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("Word"),
            new DocumentMetadata(2048, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "en"),
            new Provenance("https://example.com/file.docx", "Example"),
            new CorrelationId("corr-24"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.docx"));
        document.RecordDetectedDocumentType(new DocumentType("Docx"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.Equal("Sample DOCX text from the document body", result.ExtractedText?.Value);
    }

    [Fact]
    public void Execute_SkipsExtractionForRejectedDocument()
    {
        var extractionService = new TestTextExtractionService("new text", "Pdf");
        var useCase = new ExtractTextUseCase(extractionService);

        var document = Document.Reject(
            new DocumentId("doc-24"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-24"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"),
            new RejectionReason("upstream rejection"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.Equal(IngestionState.Rejected, document.State);
        Assert.Equal(IngestionOutcome.Rejected, document.Outcome);
        Assert.Null(document.ExtractedText);
    }

    [Fact]
    public void ServiceCollectionRegistration_ResolvesExtractTextUseCaseWithStrategyAwareRouter()
    {
        var services = new ServiceCollection();
        services.AddDocumentIngestionApplication();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var useCase = provider.GetRequiredService<ExtractTextUseCase>();

        var document = Document.Accept(
            new DocumentId("doc-25"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("Word"),
            new DocumentMetadata(2048, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "en"),
            new Provenance("https://example.com/file.docx", "Example"),
            new CorrelationId("corr-25"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.docx"));
        document.RecordDetectedDocumentType(new DocumentType("Docx"));

        var result = useCase.Execute(document);

        Assert.Equal("Sample DOCX text from the document body", result.ExtractedText?.Value);
    }

    [Fact]
    public void ServiceCollectionRegistration_WiresNormalizationIntoExtractionFlow()
    {
        var services = new ServiceCollection();
        services.AddDocumentIngestionApplication();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var useCase = provider.GetRequiredService<ExtractTextUseCase>();
        var normalizationService = provider.GetRequiredService<ITextNormalizationService>();

        var document = Document.Accept(
            new DocumentId("doc-27"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-27"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = useCase.Execute(document);
        var expectedNormalizedText = normalizationService.Normalize("Sample PDF text from page 1\nSample PDF text from page 2", document).NormalizedText;

        Assert.True(result.HasNormalizedText);
        Assert.Equal(expectedNormalizedText, result.NormalizedText?.Value);
    }

    [Fact]
    public void ServiceCollectionRegistration_ResolvesClauseDetectionUseCase()
    {
        var services = new ServiceCollection();
        services.AddDocumentIngestionApplication();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var useCase = provider.GetRequiredService<DetectClausesUseCase>();

        Assert.NotNull(useCase);
    }

    [Fact]
    public void ServiceCollectionRegistration_ResolvesClauseCategorizationUseCase()
    {
        var services = new ServiceCollection();
        services.AddDocumentIngestionApplication();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var service = provider.GetRequiredService<IClauseCategorizationService>();

        Assert.NotNull(service);
    }

    private sealed class TestTextExtractionService : ITextExtractionService
    {
        private readonly string _text;
        private readonly string _strategy;

        public TestTextExtractionService(string text, string strategy)
        {
            _text = text;
            _strategy = strategy;
        }

        public TextExtractionResult Extract(string content, Document document)
        {
            return new TextExtractionResult(_text, _strategy);
        }
    }

    private sealed class ThrowingTextExtractionService : ITextExtractionService
    {
        public TextExtractionResult Extract(string content, Document document)
        {
            throw new InvalidOperationException("boom");
        }
    }

    private sealed class TestTextNormalizationService : ITextNormalizationService
    {
        private readonly string _normalizedText;

        public TestTextNormalizationService(string normalizedText)
        {
            _normalizedText = normalizedText;
        }

        public TextNormalizationResult Normalize(string content, Document document)
        {
            return new TextNormalizationResult(_normalizedText, "TestNormalization");
        }
    }

    private sealed class UnexpectedExceptionTextExtractionService : ITextExtractionService
    {
        public TextExtractionResult Extract(string content, Document document)
        {
            throw new ArgumentException("unexpected");
        }
    }

    private sealed class ThrowingClauseCategorizationService : IClauseCategorizationService
    {
        public ClauseCategorizationResult Categorize(IReadOnlyList<Clause> clauses)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
