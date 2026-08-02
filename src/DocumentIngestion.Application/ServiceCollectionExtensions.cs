using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentIngestion.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocumentIngestionApplication(this IServiceCollection services)
    {
        return services.AddDocumentIngestionApplication(configuration: null);
    }

    public static IServiceCollection AddDocumentIngestionApplication(this IServiceCollection services, IConfiguration? configuration)
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
        services.AddSingleton<IDocumentRepository>(sp =>
        {
            var connectionString = configuration?.GetConnectionString("DocumentIngestion")
                ?? Environment.GetEnvironmentVariable("DOCUMENT_INGESTION_CONNECTION_STRING")
                ?? "Data Source=document-ingestion.db";

            return LooksLikePostgresConnectionString(connectionString)
                ? new PostgresDocumentRepository(connectionString)
                : new SqliteDocumentRepository(connectionString);
        });
        services.AddSingleton<IIngestionEventPublisher, DocumentIngestionEventPublisher>();
        services.AddSingleton<RepositoryOperationLogger>();
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
            sp.GetRequiredService<CategorizeClausesUseCase>(),
            sp.GetRequiredService<ClassifyDocumentUseCase>(),
            sp.GetRequiredService<GenerateDocumentSummaryUseCase>(),
            sp.GetRequiredService<GenerateDocumentEmbeddingUseCase>()));
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
        services.AddTransient<ClassifyDocumentUseCase>(sp => new ClassifyDocumentUseCase(
            null,
            null,
            sp.GetRequiredService<IIngestionEventPublisher>()));
        services.AddTransient<GenerateDocumentSummaryUseCase>(sp => new GenerateDocumentSummaryUseCase(
            null,
            null,
            sp.GetRequiredService<IIngestionEventPublisher>()));
        services.AddTransient<GenerateDocumentEmbeddingUseCase>(sp => new GenerateDocumentEmbeddingUseCase(
            null,
            null,
            sp.GetRequiredService<IIngestionEventPublisher>()));
        services.AddTransient<IngestDocumentUseCase>();
        services.AddTransient<DocumentIngestionEndpoint>();

        return services;
    }

    private static bool LooksLikePostgresConnectionString(string connectionString)
    {
        return connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("User ID=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase);
    }
}
