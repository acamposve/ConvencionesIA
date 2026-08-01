using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Domain.Tests;

public class DocumentSummaryTests
{
    [Fact]
    public void RecordDocumentSummary_TransitionsDocumentToSummarizedStage()
    {
        var document = Document.Accept(
            new DocumentId("doc-80"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-80"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordDocumentSummary(DocumentSummaryResult.Create(
            new SummaryText("This document outlines the key obligations and parties.")));

        Assert.True(document.HasDocumentSummary);
        Assert.Equal(ProcessingStage.DocumentSummarized, document.ProcessingStage);
        Assert.Equal(IngestionState.Accepted, document.State);
        Assert.Equal(IngestionOutcome.Accepted, document.Outcome);
    }

    [Fact]
    public void RecordDocumentSummary_RejectsEmptySummaryText()
    {
        var exception = Assert.Throws<DomainValidationException>(() => DocumentSummaryResult.Create(
            new SummaryText("   ")));

        Assert.Equal("Summary text is required.", exception.Message);
    }

    [Fact]
    public void RecordDocumentSummary_RejectsDuplicateSummaryRecording()
    {
        var document = Document.Accept(
            new DocumentId("doc-81"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-81"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordDocumentSummary(DocumentSummaryResult.Create(
            new SummaryText("First summary")));

        var exception = Assert.Throws<InvalidOperationException>(() => document.RecordDocumentSummary(
            DocumentSummaryResult.Create(new SummaryText("Second summary"))));

        Assert.Equal("Document summary can only be recorded once.", exception.Message);
    }

    [Fact]
    public void FailDocumentSummary_TransitionsDocumentToFailedState()
    {
        var document = Document.Accept(
            new DocumentId("doc-82"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-82"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.FailDocumentSummary("Document summary failed");

        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(ProcessingStage.None, document.ProcessingStage);
        Assert.Equal(IngestionOutcome.Failed, document.Outcome);
        Assert.Equal("Document summary failed", document.RejectionReason?.Value);
    }
}
