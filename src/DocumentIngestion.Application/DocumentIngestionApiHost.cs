using DocumentIngestion.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentIngestion.Application;

public static class DocumentIngestionApiHost
{
    public static WebApplication BuildApp(string[]? args = null, bool useTestServer = false)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        if (useTestServer)
        {
            builder.WebHost.UseTestServer();
        }

        builder.Services.AddDocumentIngestionApplication();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        app.MapPost("/api/v1/documents/ingestion", (IngestDocumentRequestContract request, HttpContext httpContext, IServiceProvider services) =>
        {
            var endpoint = services.GetRequiredService<DocumentIngestionEndpoint>();
            var demoUserId = httpContext.Request.Headers["x-demo-user"].FirstOrDefault() ?? "demo-user";
            var demoTenantId = httpContext.Request.Headers["x-demo-tenant"].FirstOrDefault() ?? request.TenantId;
            var response = endpoint.Handle(request, userId: demoUserId, callerTenantId: demoTenantId);
            return Results.Ok(response);
        });

        app.MapGet("/api/v1/documents/{id}", (string id, IServiceProvider services) =>
        {
            var repository = services.GetRequiredService<IDocumentRepository>();
            var document = repository.GetById(id);
            if (document is null)
            {
                return Results.NotFound(new { error = "Document not found." });
            }

            return Results.Ok(ToPersistenceContract(document));
        });

        app.MapGet("/api/v1/documents", (string? tenantId, IServiceProvider services, int page = 1, int pageSize = 10) =>
        {
            var repository = services.GetRequiredService<IDocumentRepository>();
            var documents = repository switch
            {
                InMemoryDocumentRepository memoryRepository => memoryRepository.GetAll(tenantId, page, pageSize),
                _ => Array.Empty<DocumentPersistenceContract>()
            };

            return Results.Ok(documents);
        });

        return app;
    }

    public static DocumentPersistenceContract ToPersistenceContract(Document document)
    {
        return new DocumentPersistenceContract(
            document.Id.Value,
            document.TenantId.Value,
            document.Source.Value,
            document.Format.Value,
            document.Metadata.FileSizeBytes,
            document.Metadata.MimeType,
            document.Metadata.Language,
            document.Metadata.PageCount,
            document.Metadata.Author,
            document.Metadata.CreationDate,
            document.Provenance.SourceReference,
            document.Provenance.SourceName,
            document.CorrelationId.Value,
            document.IdempotencyKey.Value,
            document.Outcome?.ToString(),
            document.RejectionReason?.Value,
            document.ProcessingStage.ToString(),
            document.State.ToString(),
            document.DetectedDocumentType?.Value,
            document.ExtractedText?.Value,
            document.NormalizedText?.Value,
            document.Revisions.Select(revision => new DocumentRevisionPersistenceContract(
                revision.Version,
                revision.Timestamp,
                revision.Outcome.ToString(),
                revision.ProcessingStage.ToString())).ToList(),
            document.Clauses.Select(clause => new ClausePersistenceContract(
                clause.Id.Value,
                clause.Sequence,
                clause.NumberLabel?.Value,
                clause.Text.Value,
                clause.Span.Start,
                clause.Span.End)).ToList(),
            document.CategoryAssignments.Select(assignment => new ClauseCategoryAssignmentPersistenceContract(
                assignment.ClauseId.Value,
                assignment.CategoryCode.Value,
                assignment.ConfidenceScore.Value)).ToList(),
            document.DocumentClassification is null ? null : new List<DocumentClassificationPersistenceContract>
            {
                new(document.DocumentClassification.ClassificationCode.Value, document.DocumentClassification.ConfidenceScore.Value)
            },
            document.DocumentSummary is null ? null : new List<DocumentSummaryPersistenceContract>
            {
                new(document.DocumentSummary.SummaryText.Value)
            },
            document.DocumentEmbedding is null ? null : new List<DocumentEmbeddingPersistenceContract>
            {
                new(document.DocumentEmbedding.EmbeddingVector.Values.ToList())
            });
    }
}
