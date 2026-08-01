using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class ClausePersistenceTests
{
    [Fact]
    public void SaveAndLoad_PreservesClauseStateAndRevisionHistory()
    {
        var repository = new FileSystemDocumentRepository(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var document = Document.Accept(
            new DocumentId("doc-41"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-41"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("clause one"));
        document.RecordNormalizedText(new NormalizedText("clause one"));
        document.RecordDetectedClauses(new[]
        {
            Clause.Create(
                ClauseId.CreateDeterministic("doc-41", 1, 1),
                1,
                new ClauseText("clause one"),
                new ClauseSpan(0, 10),
                new ClauseNumberLabel("1"))
        });

        document.RecordCategoryAssignments(new[]
        {
            ClauseCategoryAssignment.Create(
                ClauseId.CreateDeterministic("doc-41", 1, 1),
                new ClauseCategoryCode("Obligation"),
                new ConfidenceScore(0.90m))
        });

        repository.Save(document);
        var reloaded = repository.GetById(document.Id.Value);

        Assert.NotNull(reloaded);
        Assert.True(reloaded!.HasClauses);
        Assert.True(reloaded.HasCategoryAssignments);
        Assert.Equal(1, reloaded.Clauses.Count);
        Assert.Equal("1", reloaded.Clauses[0].NumberLabel?.Value);
        Assert.Equal("clause one", reloaded.Clauses[0].Text.Value);
        Assert.Equal(ProcessingStage.ClausesCategorized, reloaded.ProcessingStage);
        Assert.Equal(document.Revisions.Count, reloaded.Revisions.Count);
        Assert.Equal("Obligation", reloaded.CategoryAssignments[0].CategoryCode.Value);
        Assert.Equal(0.90m, reloaded.CategoryAssignments[0].ConfidenceScore.Value);
    }
}
