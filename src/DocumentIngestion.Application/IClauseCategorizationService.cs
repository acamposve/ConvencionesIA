using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public interface IClauseCategorizationService
{
    ClauseCategorizationResult Categorize(IReadOnlyList<Clause> clauses);
}

public sealed record ClauseCategorizationResult(IReadOnlyList<ClauseCategoryAssignment> Assignments);
