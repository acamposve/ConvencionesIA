using DocumentIngestion.Application;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class BoundaryClauseDetectionServiceTests
{
    [Fact]
    public void Detect_PreservesOrderingAndNumberLabels()
    {
        var service = new BoundaryClauseDetectionService();
        var result = service.Detect("1. First clause. 2. Second clause.");

        Assert.Equal(2, result.Clauses.Count);
        Assert.Equal(1, result.Clauses[0].Sequence);
        Assert.Equal("1", result.Clauses[0].NumberLabel);
        Assert.Equal("First clause", result.Clauses[0].Text);
        Assert.Equal(2, result.Clauses[1].Sequence);
        Assert.Equal("2", result.Clauses[1].NumberLabel);
        Assert.Equal("Second clause", result.Clauses[1].Text);
    }

    [Fact]
    public void Detect_ReturnsEmptyResultForBlankText()
    {
        var service = new BoundaryClauseDetectionService();
        var result = service.Detect("   ");

        Assert.Empty(result.Clauses);
    }
}
