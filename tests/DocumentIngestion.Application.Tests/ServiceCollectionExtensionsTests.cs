using DocumentIngestion.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDocumentIngestionApplication_RegistersSqliteRepository_WhenConnectionStringConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DocumentIngestion"] = "Data Source=:memory:"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddDocumentIngestionApplication(configuration);

        using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService<IDocumentRepository>();

        Assert.IsType<SqliteDocumentRepository>(repository);
    }
}
