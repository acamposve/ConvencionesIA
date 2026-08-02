using DocumentIngestion.Application;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public sealed class PostgresDocumentRepositoryTests
{
    [Fact]
    public void Constructor_StoresConnectionStringWithoutThrowing()
    {
        var repository = new PostgresDocumentRepository("Host=localhost;Username=test;Password=test;Database=ingestion");

        Assert.NotNull(repository);
    }
}
