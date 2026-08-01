using Microsoft.Extensions.DependencyInjection;

namespace DocumentIngestion.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentIngestionApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDocumentTypeDetectionService, MimeTypeDocumentTypeDetectionService>();
        services.AddSingleton<PdfTextExtractionService>();
        services.AddSingleton<DocxTextExtractionService>();
        services.AddSingleton<ImageOcrTextExtractionService>();
        services.AddSingleton<TextExtractionServiceRouter>(sp => new TextExtractionServiceRouter(
            sp.GetRequiredService<PdfTextExtractionService>(),
            sp.GetRequiredService<DocxTextExtractionService>(),
            sp.GetRequiredService<ImageOcrTextExtractionService>()));
        services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        services.AddSingleton<IIngestionEventPublisher, DocumentIngestionEventPublisher>();
        services.AddSingleton<DocumentTypeDetectionEventPublisher>();
        services.AddSingleton<ITextNormalizationService, OcrTextNormalizationService>();
        services.AddSingleton<IClauseDetectionService, BoundaryClauseDetectionService>();
        services.AddSingleton<IClauseCategorizationService, BoundaryClauseCategorizationService>();
        services.AddTransient<DetectDocumentTypeUseCase>(sp => new DetectDocumentTypeUseCase(
            sp.GetRequiredService<IDocumentTypeDetectionService>(),
            null,
            sp.GetRequiredService<DocumentTypeDetectionEventPublisher>()));
        services.AddTransient<ExtractTextUseCase>(sp => new ExtractTextUseCase(
            sp.GetRequiredService<TextExtractionServiceRouter>(),
            null,
            sp.GetRequiredService<IIngestionEventPublisher>(),
            sp.GetRequiredService<NormalizeTextUseCase>(),
            sp.GetRequiredService<DetectClausesUseCase>(),
            sp.GetRequiredService<CategorizeClausesUseCase>()));
        services.AddTransient<NormalizeTextUseCase>(sp => new NormalizeTextUseCase(
            sp.GetRequiredService<ITextNormalizationService>(),
            null,
            sp.GetRequiredService<IIngestionEventPublisher>()));
        services.AddTransient<DetectClausesUseCase>(sp => new DetectClausesUseCase(
            sp.GetRequiredService<IClauseDetectionService>(),
            null,
            sp.GetRequiredService<IIngestionEventPublisher>()));
        services.AddTransient<CategorizeClausesUseCase>(sp => new CategorizeClausesUseCase(
            sp.GetRequiredService<IClauseCategorizationService>(),
            null,
            sp.GetRequiredService<IIngestionEventPublisher>()));
        services.AddTransient<IngestDocumentUseCase>();
        services.AddTransient<DocumentIngestionEndpoint>();

        return services;
    }
}
