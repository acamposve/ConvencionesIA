using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Domain.Tests;

public class ClauseTests
{
    [Fact]
    public void Create_StoresValidClauseState()
    {
        var clause = Clause.Create(
            ClauseId.CreateDeterministic("doc-1", 1, 1),
            1,
            new ClauseText("This is a clause."),
            new ClauseSpan(0, 19),
            new ClauseNumberLabel("1"));

        Assert.Equal(1, clause.Sequence);
        Assert.Equal("doc-1:1:1", clause.Id.Value);
        Assert.Equal("1", clause.NumberLabel?.Value);
        Assert.Equal("This is a clause.", clause.Text.Value);
        Assert.Equal(0, clause.Span.Start);
        Assert.Equal(19, clause.Span.End);
    }

    [Fact]
    public void Create_ThrowsForNonPositiveSequence()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Clause.Create(
            ClauseId.CreateDeterministic("doc-1", 1, 1),
            0,
            new ClauseText("This is a clause."),
            new ClauseSpan(0, 19)));

        Assert.Equal("Clause sequence must be greater than zero.", ex.Message);
    }

    [Fact]
    public void Create_ThrowsForInvalidSpan()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Clause.Create(
            ClauseId.CreateDeterministic("doc-1", 1, 1),
            1,
            new ClauseText("This is a clause."),
            new ClauseSpan(10, 5)));

        Assert.Equal("Clause span is invalid.", ex.Message);
    }

    [Fact]
    public void Create_ThrowsForBlankText()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Clause.Create(
            ClauseId.CreateDeterministic("doc-1", 1, 1),
            1,
            new ClauseText("   "),
            new ClauseSpan(0, 5)));

        Assert.Equal("Clause text is required.", ex.Message);
    }

    [Fact]
    public void CreateDeterministic_ProducesStableClauseIdentifier()
    {
        var id = ClauseId.CreateDeterministic("doc-1", 1, 2);

        Assert.Equal("doc-1:1:2", id.Value);
    }

    [Fact]
    public void CreateDeterministic_ThrowsForInvalidIdentityComponents()
    {
        var ex = Assert.Throws<ArgumentException>(() => ClauseId.CreateDeterministic("", 1, 1));

        Assert.Contains("documentId", ex.Message);
    }
}
