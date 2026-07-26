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
        services.AddTransient<DetectDocumentTypeUseCase>(sp => new DetectDocumentTypeUseCase(
            sp.GetRequiredService<IDocumentTypeDetectionService>(),
            null,
            sp.GetRequiredService<DocumentTypeDetectionEventPublisher>()));
        services.AddTransient<ExtractTextUseCase>(sp => new ExtractTextUseCase(
            sp.GetRequiredService<TextExtractionServiceRouter>(),
            null,
            sp.GetRequiredService<IIngestionEventPublisher>()));
        services.AddTransient<IngestDocumentUseCase>();
        services.AddTransient<DocumentIngestionEndpoint>();

        return services;
    }
}
