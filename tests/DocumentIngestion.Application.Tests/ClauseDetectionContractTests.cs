using DocumentIngestion.Application;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class ClauseDetectionContractTests
{
    [Fact]
    public void DetectionResult_PreservesOrderedClausesAndOptionalLabels()
    {
        var result = new ClauseDetectionResult(
            new List<DetectedClause>
            {
                new(1, "1", "First clause", 0, 12),
                new(2, null, "Second clause", 13, 27)
            });

        Assert.Equal(2, result.Clauses.Count);
        Assert.Equal(1, result.Clauses[0].Sequence);
        Assert.Equal("1", result.Clauses[0].NumberLabel);
        Assert.Equal("First clause", result.Clauses[0].Text);
        Assert.Equal(0, result.Clauses[0].SpanStart);
        Assert.Equal(12, result.Clauses[0].SpanEnd);
        Assert.Equal(2, result.Clauses[1].Sequence);
        Assert.Null(result.Clauses[1].NumberLabel);
    }

    [Fact]
    public void DetectionService_Contract_IsUsableAsApplicationAbstraction()
    {
        var service = new TestClauseDetectionService();
        var result = service.Detect("Clause one. Clause two.");

        Assert.Equal(2, result.Clauses.Count);
        Assert.Equal("Clause one.", result.Clauses[0].Text);
        Assert.Equal("Clause two.", result.Clauses[1].Text);
    }

    private sealed class TestClauseDetectionService : IClauseDetectionService
    {
        public ClauseDetectionResult Detect(string normalizedText)
        {
            return new ClauseDetectionResult(
                new List<DetectedClause>
                {
                    new(1, null, "Clause one.", 0, 12),
                    new(2, null, "Clause two.", 13, 25)
                });
        }
    }
}
