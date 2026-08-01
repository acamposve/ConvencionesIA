using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Domain.Tests;

public class DocumentTests
{
    [Fact]
    public void Accept_CreatesAcceptedDocumentWithPendingProcessingStage()
    {
        var document = Document.Accept(
            new DocumentId("doc-1"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-1"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        Assert.Equal(IngestionState.Accepted, document.State);
        Assert.Equal(ProcessingStage.PendingProcessing, document.ProcessingStage);
        Assert.Equal(IngestionOutcome.Accepted, document.Outcome);
        Assert.Null(document.RejectionReason);
        Assert.NotEmpty(document.Revisions);
    }

    [Fact]
    public void Reject_CreatesRejectedDocumentWithReason()
    {
        var document = Document.Reject(
            new DocumentId("doc-2"),
            new TenantId("tenant-1"),
            new DocumentSource("URL"),
            new DocumentFormat("DOCX"),
            new DocumentMetadata(512, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "en"),
            new Provenance("https://example.com/file.docx", "Example"),
            new CorrelationId("corr-2"),
            new IdempotencyKey("tenant-1|url|https://example.com/file.docx"),
            new RejectionReason("Unsupported format"));

        Assert.Equal(IngestionState.Rejected, document.State);
        Assert.Equal(ProcessingStage.None, document.ProcessingStage);
        Assert.Equal(IngestionOutcome.Rejected, document.Outcome);
        Assert.Equal("Unsupported format", document.RejectionReason?.Value);
    }

    [Fact]
    public void Accept_ThrowsForUnsupportedFormat()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Accept(
            new DocumentId("doc-3"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("TXT"),
            new DocumentMetadata(1024, "text/plain", "en"),
            new Provenance("https://example.com/file.txt", "Example"),
            new CorrelationId("corr-3"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.txt")));

        Assert.Equal("Unsupported format", ex.Message);
    }

    [Fact]
    public void Accept_ThrowsForMissingRequiredMetadata()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Accept(
            new DocumentId("doc-4"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(0, string.Empty, "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-4"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf")));

        Assert.Equal("Validation failure", ex.Message);
    }

    [Fact]
    public void Accept_ThrowsForUnsupportedSource()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Accept(
            new DocumentId("doc-5"),
            new TenantId("tenant-1"),
            new DocumentSource("Email"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(1024, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-5"),
            new IdempotencyKey("tenant-1|email|https://example.com/file.pdf")));

        Assert.Equal("Unsupported source", ex.Message);
    }

    [Fact]
    public void Accept_ThrowsForMissingProvenanceReference()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Accept(
            new DocumentId("doc-6"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(1024, "application/pdf", "en"),
            new Provenance(string.Empty, "Example"),
            new CorrelationId("corr-6"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf")));

        Assert.Equal("Validation failure", ex.Message);
    }

    [Fact]
    public void Accept_ThrowsForEmptyIdempotencyKey()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Accept(
            new DocumentId("doc-7"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(1024, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-7"),
            new IdempotencyKey(string.Empty)));

        Assert.Equal("Validation failure", ex.Message);
    }

    [Fact]
    public void Accept_ThrowsForMissingTenantContext()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Accept(
            new DocumentId("doc-8"),
            new TenantId("   "),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(1024, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-8"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf")));

        Assert.Equal("Invalid tenant context", ex.Message);
    }

    [Fact]
    public void Reject_ThrowsForMissingTenantContext()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Document.Reject(
            new DocumentId("doc-9"),
            new TenantId("   "),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(1024, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-9"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"),
            new RejectionReason("Unsupported format")));

        Assert.Equal("Invalid tenant context", ex.Message);
    }

    [Fact]
    public void DocumentType_AllowsSupportedValues()
    {
        var documentType = new DocumentType("Pdf");

        Assert.Equal("Pdf", documentType.Value);
    }

    [Fact]
    public void DocumentType_NormalizesKnownValues()
    {
        var documentType = new DocumentType("docx");

        Assert.Equal("Docx", documentType.Value);
    }

    [Fact]
    public void DocumentType_ThrowsForUnsupportedValue()
    {
        var ex = Assert.Throws<DomainValidationException>(() => new DocumentType("Xls"));

        Assert.Equal("Unsupported document type", ex.Message);
    }

    [Fact]
    public void DocumentType_ThrowsForBlankValue()
    {
        var ex = Assert.Throws<DomainValidationException>(() => new DocumentType("   "));

        Assert.Equal("Unsupported document type", ex.Message);
    }

    [Fact]
    public void RecordDetectedDocumentType_SetsDetectedType()
    {
        var document = Document.Accept(
            new DocumentId("doc-10"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-10"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordDetectedDocumentType(new DocumentType("Pdf"));

        Assert.True(document.HasDetectedDocumentType);
        Assert.Equal("Pdf", document.DetectedDocumentType?.Value);
    }

    [Fact]
    public void RecordDetectedDocumentType_ThrowsWhenCalledTwice()
    {
        var document = Document.Accept(
            new DocumentId("doc-11"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-11"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordDetectedDocumentType(new DocumentType("Pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => document.RecordDetectedDocumentType(new DocumentType("Docx")));

        Assert.Equal("Document type detection can only be recorded once.", ex.Message);
    }

    [Fact]
    public void RecordDetectedDocumentType_ThrowsAfterDocumentIsRejected()
    {
        var document = Document.Reject(
            new DocumentId("doc-12"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-12"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"),
            new RejectionReason("Unsupported format"));

        var ex = Assert.Throws<InvalidOperationException>(() => document.RecordDetectedDocumentType(new DocumentType("Pdf")));

        Assert.Equal("Cannot record document type after the document has been rejected or failed.", ex.Message);
    }

    [Fact]
    public void RecordExtractedText_SetsRawText()
    {
        var document = Document.Accept(
            new DocumentId("doc-13"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-13"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("hello world"));

        Assert.True(document.HasExtractedText);
        Assert.Equal("hello world", document.ExtractedText?.Value);
    }

    [Fact]
    public void FailExtraction_TransitionsDocumentToFailedState()
    {
        var document = Document.Accept(
            new DocumentId("doc-14"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-14"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.FailExtraction("Extraction failed");

        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(IngestionOutcome.Failed, document.Outcome);
        Assert.Equal("Extraction failed", document.RejectionReason?.Value);
    }

    [Fact]
    public void RecordExtractedText_ThrowsWhenTextHasAlreadyBeenRecorded()
    {
        var document = Document.Accept(
            new DocumentId("doc-15"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-15"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("hello"));

        var ex = Assert.Throws<InvalidOperationException>(() => document.RecordExtractedText(new RawText("again")));

        Assert.Equal("Extracted text can only be recorded once.", ex.Message);
    }

    [Fact]
    public void FailExtraction_ThrowsWhenDocumentIsAlreadyRejected()
    {
        var document = Document.Reject(
            new DocumentId("doc-16"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-16"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"),
            new RejectionReason("Unsupported format"));

        var ex = Assert.Throws<InvalidOperationException>(() => document.FailExtraction("Extraction failed"));

        Assert.Equal("Cannot fail extraction after the document has been rejected or failed.", ex.Message);
    }

    [Fact]
    public void RecordNormalizedText_SetsNormalizedText()
    {
        var document = Document.Accept(
            new DocumentId("doc-17"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-17"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("hello world"));
        document.RecordNormalizedText(new NormalizedText("hello world"));

        Assert.True(document.HasNormalizedText);
        Assert.Equal("hello world", document.NormalizedText?.Value);
    }

    [Fact]
    public void RecordNormalizedText_ThrowsWhenExtractedTextIsMissing()
    {
        var document = Document.Accept(
            new DocumentId("doc-18"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-18"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => document.RecordNormalizedText(new NormalizedText("hello world")));

        Assert.Equal("Cannot record normalized text before extracted text is available.", ex.Message);
    }

    [Fact]
    public void RecordNormalizedText_ThrowsWhenTextHasAlreadyBeenRecorded()
    {
        var document = Document.Accept(
            new DocumentId("doc-19"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-19"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("hello"));
        document.RecordNormalizedText(new NormalizedText("hello"));

        var ex = Assert.Throws<InvalidOperationException>(() => document.RecordNormalizedText(new NormalizedText("again")));

        Assert.Equal("Normalized text can only be recorded once.", ex.Message);
    }

    [Fact]
    public void RecordNormalizedText_PreservesOriginalExtractedText()
    {
        var document = Document.Accept(
            new DocumentId("doc-20"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-20"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("original OCR text"));
        document.RecordNormalizedText(new NormalizedText("normalized OCR text"));

        Assert.Equal("original OCR text", document.ExtractedText?.Value);
        Assert.Equal("normalized OCR text", document.NormalizedText?.Value);
    }

    [Fact]
    public void RecordDetectedClauses_TransitionsDocumentToClausesDetectedStage()
    {
        var document = Document.Accept(
            new DocumentId("doc-21"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-21"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("Clause one. Clause two."));
        document.RecordNormalizedText(new NormalizedText("Clause one. Clause two."));

        var clauses = new[]
        {
            Clause.Create(
                ClauseId.CreateDeterministic("doc-21", 1, 1),
                1,
                new ClauseText("Clause one."),
                new ClauseSpan(0, 12))
        };

        document.RecordDetectedClauses(clauses);

        Assert.True(document.HasClauses);
        Assert.Equal(ProcessingStage.ClausesDetected, document.ProcessingStage);
        Assert.Equal(IngestionState.Accepted, document.State);
        Assert.Equal(IngestionOutcome.Accepted, document.Outcome);
        Assert.NotNull(document.Clauses.Single());
    }

    [Fact]
    public void FailClauseDetection_TransitionsDocumentToFailedState()
    {
        var document = Document.Accept(
            new DocumentId("doc-22"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-22"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("Clause one."));
        document.RecordNormalizedText(new NormalizedText("Clause one."));
        document.FailClauseDetection("Clause detection failed");

        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(ProcessingStage.None, document.ProcessingStage);
        Assert.Equal(IngestionOutcome.Failed, document.Outcome);
        Assert.Equal("Clause detection failed", document.RejectionReason?.Value);
    }

    [Fact]
    public void RecordDetectedClauses_ThrowsWhenClausesAreAlreadyRecorded()
    {
        var document = Document.Accept(
            new DocumentId("doc-23"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-23"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("Clause one."));
        document.RecordNormalizedText(new NormalizedText("Clause one."));
        document.RecordDetectedClauses(new[]
        {
            Clause.Create(
                ClauseId.CreateDeterministic("doc-23", 1, 1),
                1,
                new ClauseText("Clause one."),
                new ClauseSpan(0, 12))
        });

        var ex = Assert.Throws<DomainValidationException>(() => document.RecordDetectedClauses(Array.Empty<Clause>()));

        Assert.Equal("At least one clause is required.", ex.Message);
    }
}
