using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Domain.Tests;

public class ClauseCategorizationTests
{
    [Fact]
    public void RecordCategoryAssignments_TransitionsDocumentToClausesCategorizedStage()
    {
        var document = Document.Accept(
            new DocumentId("doc-60"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-60"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("Clause one."));
        document.RecordNormalizedText(new NormalizedText("Clause one."));
        document.RecordDetectedClauses(new[]
        {
            Clause.Create(
                ClauseId.CreateDeterministic("doc-60", 1, 1),
                1,
                new ClauseText("Clause one."),
                new ClauseSpan(0, 12))
        });

        document.RecordCategoryAssignments(new[]
        {
            ClauseCategoryAssignment.Create(
                ClauseId.CreateDeterministic("doc-60", 1, 1),
                new ClauseCategoryCode("Obligation"),
                new ConfidenceScore(0.95m))
        });

        Assert.True(document.HasCategoryAssignments);
        Assert.Equal(ProcessingStage.ClausesCategorized, document.ProcessingStage);
        Assert.Equal(IngestionState.Accepted, document.State);
        Assert.Equal(IngestionOutcome.Accepted, document.Outcome);
    }

    [Fact]
    public void RecordCategoryAssignments_RejectsEmptyCategoryCode()
    {
        var exception = Assert.Throws<DomainValidationException>(() => ClauseCategoryAssignment.Create(
            ClauseId.CreateDeterministic("doc-62", 1, 1),
            new ClauseCategoryCode(" "),
            new ConfidenceScore(0.95m)));

        Assert.Equal("Category code is required.", exception.Message);
    }

    [Fact]
    public void RecordCategoryAssignments_RejectsOutOfRangeConfidenceScore()
    {
        var exception = Assert.Throws<DomainValidationException>(() => ClauseCategoryAssignment.Create(
            ClauseId.CreateDeterministic("doc-63", 1, 1),
            new ClauseCategoryCode("Obligation"),
            new ConfidenceScore(1.5m)));

        Assert.Equal("Confidence score must be between 0.0 and 1.0.", exception.Message);
    }

    [Fact]
    public void RecordCategoryAssignments_RequiresAssignmentsForDetectedClauses()
    {
        var document = Document.Accept(
            new DocumentId("doc-64"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-64"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("Clause one."));
        document.RecordNormalizedText(new NormalizedText("Clause one."));
        document.RecordDetectedClauses(new[]
        {
            Clause.Create(
                ClauseId.CreateDeterministic("doc-64", 1, 1),
                1,
                new ClauseText("Clause one."),
                new ClauseSpan(0, 12))
        });

        var exception = Assert.Throws<DomainValidationException>(() => document.RecordCategoryAssignments(new[]
        {
            ClauseCategoryAssignment.Create(
                ClauseId.CreateDeterministic("doc-64", 1, 2),
                new ClauseCategoryCode("Obligation"),
                new ConfidenceScore(0.95m))
        }));

        Assert.Equal("Category assignments must reference detected clauses.", exception.Message);
    }

    [Fact]
    public void FailClauseCategorization_TransitionsDocumentToFailedState()
    {
        var document = Document.Accept(
            new DocumentId("doc-61"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-61"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("Clause one."));
        document.RecordNormalizedText(new NormalizedText("Clause one."));
        document.FailClauseCategorization("Clause categorization failed");

        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(ProcessingStage.None, document.ProcessingStage);
        Assert.Equal(IngestionOutcome.Failed, document.Outcome);
        Assert.Equal("Clause categorization failed", document.RejectionReason?.Value);
    }
}
