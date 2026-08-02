using System.Net;
using System.Net.Http.Json;
using DocumentIngestion.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocumentIngestionApiIntegrationTests
{
    private static HttpClient CreateClient()
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
            {
                builder.UseTestServer();
                builder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddDocumentIngestionApplication();
                });
                builder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }));
                        endpoints.MapPost("/api/v1/documents/ingestion", (IngestDocumentRequestContract request, HttpContext httpContext, IServiceProvider services) =>
                        {
                            var endpoint = services.GetRequiredService<DocumentIngestionEndpoint>();
                            var response = endpoint.Handle(request, userId: httpContext.Request.Headers["x-demo-user"].FirstOrDefault(), callerTenantId: httpContext.Request.Headers["x-demo-tenant"].FirstOrDefault());
                            return Results.Ok(response);
                        });
                        endpoints.MapGet("/api/v1/documents/{id}", (string id, IServiceProvider services) =>
                        {
                            var repository = services.GetRequiredService<IDocumentRepository>();
                            var document = repository.GetById(id);
                            return document is null ? Results.NotFound(new { error = "Document not found." }) : Results.Ok(DocumentIngestionApiHost.ToPersistenceContract(document));
                        });
                        endpoints.MapGet("/api/v1/documents", (string? tenantId, IServiceProvider services, int page = 1, int pageSize = 10) =>
                        {
                            var repository = services.GetRequiredService<IDocumentRepository>();
                            var documents = repository.GetAll(tenantId, page, pageSize);
                            return Results.Ok(documents);
                        });
                    });
                });
            })
            .Build();

        host.StartAsync().GetAwaiter().GetResult();

        var client = host.GetTestClient();
        client.BaseAddress = new Uri("http://localhost");
        return client;
    }

    [Fact]
    public async Task PostIngestion_ReturnsAcceptedResponseForValidRequest()
    {
        using var client = CreateClient();

        var request = new IngestDocumentRequestContract(
            "tenant-api",
            "Upload",
            "PDF",
            2048,
            "application/pdf",
            "en",
            1,
            "Alice",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "https://example.com/file.pdf",
            "corr-post-1",
            "tenant-api|upload|https://example.com/file.pdf");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/documents/ingestion")
        {
            Content = JsonContent.Create(request)
        };
        requestMessage.Headers.Add("x-demo-user", "demo-user");
        requestMessage.Headers.Add("x-demo-tenant", request.TenantId);

        var response = await client.SendAsync(requestMessage);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IngestDocumentResponseContract>();
        Assert.NotNull(payload);
        Assert.Equal("Accepted", payload!.Outcome);
        Assert.Equal("PendingProcessing", payload.ProcessingStage);
        Assert.Equal("corr-post-1", payload.CorrelationId);
    }

    [Fact]
    public async Task GetDocumentById_ReturnsPersistedDocumentState()
    {
        using var client = CreateClient();

        var request = new IngestDocumentRequestContract(
            "tenant-api",
            "Upload",
            "PDF",
            2048,
            "application/pdf",
            "en",
            1,
            "Alice",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "https://example.com/file.pdf",
            "corr-get-1",
            "tenant-api|upload|get-1");

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/documents/ingestion")
        {
            Content = JsonContent.Create(request)
        };
        createRequest.Headers.Add("x-demo-user", "demo-user");
        createRequest.Headers.Add("x-demo-tenant", request.TenantId);

        var created = await client.SendAsync(createRequest);
        var payload = await created.Content.ReadFromJsonAsync<IngestDocumentResponseContract>();

        Assert.NotNull(payload);

        var response = await client.GetAsync($"/api/v1/documents/{payload!.DocumentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentPersistenceContract>();
        Assert.NotNull(document);
        Assert.Equal(payload.DocumentId, document!.Id);
        Assert.Equal("tenant-api", document.TenantId);
        Assert.Equal("PendingProcessing", document.ProcessingStage);
    }

    [Fact]
    public async Task GetDocuments_ReturnsMatchingDocumentsForTenant()
    {
        using var client = CreateClient();

        var firstRequest = new IngestDocumentRequestContract(
            "tenant-list",
            "Upload",
            "PDF",
            2048,
            "application/pdf",
            "en",
            1,
            "Alice",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "https://example.com/file-a.pdf",
            "corr-list-1",
            "tenant-list|upload|a");

        var secondRequest = new IngestDocumentRequestContract(
            "tenant-list",
            "URL",
            "PDF",
            1024,
            "application/pdf",
            "en",
            1,
            "Bob",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "https://example.com/file-b.pdf",
            "corr-list-2",
            "tenant-list|url|b");

        using var firstMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/documents/ingestion")
        {
            Content = JsonContent.Create(firstRequest)
        };
        firstMessage.Headers.Add("x-demo-user", "demo-user");
        firstMessage.Headers.Add("x-demo-tenant", firstRequest.TenantId);
        await client.SendAsync(firstMessage);

        using var secondMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/documents/ingestion")
        {
            Content = JsonContent.Create(secondRequest)
        };
        secondMessage.Headers.Add("x-demo-user", "demo-user");
        secondMessage.Headers.Add("x-demo-tenant", secondRequest.TenantId);
        await client.SendAsync(secondMessage);

        var response = await client.GetAsync("/api/v1/documents?tenantId=tenant-list&page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var documents = await response.Content.ReadFromJsonAsync<IReadOnlyList<DocumentPersistenceContract>>();
        Assert.NotNull(documents);
        Assert.True(documents!.Count >= 2);
        Assert.All(documents, document => Assert.Equal("tenant-list", document.TenantId));
    }
}
