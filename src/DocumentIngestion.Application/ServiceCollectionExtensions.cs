using Microsoft.Extensions.DependencyInjection;

namespace DocumentIngestion.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentIngestionApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDocumentTypeDetectionService, MimeTypeDocumentTypeDetectionService>();
        services.AddSingleton<ITextExtractionService, PdfTextExtractionService>();
        services.AddSingleton<ITextExtractionService, DocxTextExtractionService>();
        services.AddSingleton<ITextExtractionService, ImageOcrTextExtractionService>();
        services.AddTransient<DetectDocumentTypeUseCase>();
        services.AddTransient<ExtractTextUseCase>();
        services.AddTransient<IngestDocumentUseCase>();
        services.AddTransient<DocumentIngestionEndpoint>();

        return services;
    }
}
