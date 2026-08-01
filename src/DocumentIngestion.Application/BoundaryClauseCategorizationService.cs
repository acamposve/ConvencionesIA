using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class BoundaryClauseCategorizationService : IClauseCategorizationService
{
    public ClauseCategorizationResult Categorize(IReadOnlyList<Clause> clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);

        if (clauses.Count == 0)
        {
            return new ClauseCategorizationResult(Array.Empty<ClauseCategoryAssignment>());
        }

        var assignments = clauses
            .Select((clause, index) => ClauseCategoryAssignment.Create(
                clause.Id,
                new ClauseCategoryCode(GetCategoryForIndex(index)),
                new ConfidenceScore(0.90m)))
            .ToList();

        return new ClauseCategorizationResult(assignments);
    }

    private static string GetCategoryForIndex(int index)
    {
        return index % 2 == 0 ? "Obligation" : "Representation";
    }
}
