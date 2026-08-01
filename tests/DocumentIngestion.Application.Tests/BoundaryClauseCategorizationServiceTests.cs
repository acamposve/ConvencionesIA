using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class BoundaryClauseCategorizationServiceTests
{
    [Fact]
    public void Categorize_ReturnsDeterministicAssignmentsForEachClause()
    {
        var service = new BoundaryClauseCategorizationService();
        var clauses = new[]
        {
            Clause.Create(
                ClauseId.CreateDeterministic("doc-70", 1, 1),
                1,
                new ClauseText("First clause"),
                new ClauseSpan(0, 12)),
            Clause.Create(
                ClauseId.CreateDeterministic("doc-70", 1, 2),
                2,
                new ClauseText("Second clause"),
                new ClauseSpan(13, 25))
        };

        var result = service.Categorize(clauses);

        Assert.Equal(2, result.Assignments.Count);
        Assert.Equal("Obligation", result.Assignments[0].CategoryCode.Value);
        Assert.Equal("Representation", result.Assignments[1].CategoryCode.Value);
        Assert.Equal(0.90m, result.Assignments[0].ConfidenceScore.Value);
        Assert.Equal(0.90m, result.Assignments[1].ConfidenceScore.Value);
        Assert.Equal(clauses[0].Id, result.Assignments[0].ClauseId);
        Assert.Equal(clauses[1].Id, result.Assignments[1].ClauseId);
    }
}
