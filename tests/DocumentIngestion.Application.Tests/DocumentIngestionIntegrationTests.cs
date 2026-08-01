using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocumentIngestionIntegrationTests
{
    [Fact]
    public void EndToEndAcceptedFlow_PersistsDocumentAndPublishesAuditRecord()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);

        var request = CreateValidRequest();
        var result = useCase.Execute(request);

        var persisted = repository.GetById(result.DocumentId);

        Assert.NotNull(persisted);
        Assert.Equal(IngestionState.Accepted, persisted!.State);
        Assert.Equal(ProcessingStage.PendingProcessing, persisted.ProcessingStage);
        Assert.Equal(IngestionOutcome.Accepted, persisted.Outcome);
        Assert.Single(publisher.AuditRecords);
        Assert.Equal("DocumentIngestionCompleted", publisher.AuditRecords[0].EventName);
        Assert.Equal("v1", publisher.AuditRecords[0].EventVersion);
    }

    [Fact]
    public void EndpointAcceptedRequest_UsesSecurityGuardAndPersistsDocument()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = CreateIngestRequestContract();
        var response = endpoint.Handle(request, userId: "user-1", callerTenantId: "tenant-1");

        var persisted = repository.GetById(response.DocumentId);

        Assert.NotNull(persisted);
        Assert.Equal("Accepted", response.Outcome);
        Assert.Equal("PendingProcessing", response.ProcessingStage);
        Assert.Equal("corr-1", response.CorrelationId);
        Assert.Equal("v1", response.Version);
        Assert.Single(publisher.AuditRecords);
    }

    [Fact]
    public void EndpointTenantMismatch_DoesNotPersistOrPublish()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = CreateIngestRequestContract();

        var ex = Assert.Throws<UnauthorizedAccessException>(() => endpoint.Handle(request, userId: "user-1", callerTenantId: "tenant-2"));

        Assert.Contains("Authorization", ex.Message);
        Assert.Null(repository.GetByTenantAndIdempotencyKey("tenant-1", "tenant-1|upload|https://example.com/file.pdf"));
        Assert.Empty(publisher.AuditRecords);
    }

    [Fact]
    public void RejectedRequest_PersistsRejectedDocumentWithoutPublishing()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);

        var request = new IngestionRequest(
            "tenant-1",
            "Email",
            "PDF",
            2048,
            "application/pdf",
            "en",
            3,
            "Example Author",
            DateTimeOffset.Parse("2024-01-01T00:00:00Z"),
            "https://example.com/file.pdf",
            "corr-2",
            "tenant-1|upload|https://example.com/file.pdf");

        var result = useCase.Execute(request);
        var persisted = repository.GetById(result.DocumentId);

        Assert.NotNull(persisted);
        Assert.Equal(IngestionState.Rejected, persisted!.State);
        Assert.Equal(IngestionOutcome.Rejected, persisted.Outcome);
        Assert.Equal("Unsupported source", persisted.RejectionReason?.Value);
        Assert.Empty(publisher.AuditRecords);
    }

    [Fact]
    public void DetectionWorkflow_PersistsDetectedTypeForSupportedMimeType()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var detectionService = new MimeTypeDocumentTypeDetectionService();
        var detectionUseCase = new DetectDocumentTypeUseCase(detectionService);

        var document = Document.Accept(
            new DocumentId("doc-15"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-15"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = detectionUseCase.Execute(document);

        Assert.True(result.HasDetectedDocumentType);
        Assert.Equal("Pdf", result.DetectedDocumentType?.Value);
    }

    [Fact]
    public void DetectionWorkflow_RejectsUnsupportedMimeType()
    {
        var detectionService = new MimeTypeDocumentTypeDetectionService();
        var detectionUseCase = new DetectDocumentTypeUseCase(detectionService);

        var document = Document.Accept(
            new DocumentId("doc-16"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/octet-stream", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-16"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<DomainValidationException>(() => detectionUseCase.Execute(document));

        Assert.Equal("Unsupported document type", ex.Message);
    }

    [Fact]
    public void ExtractTextWorkflow_RecordsExtractedTextAndUpdatesDocumentState()
    {
        var useCase = new ExtractTextUseCase(new PdfTextExtractionService());
        var document = Document.Accept(
            new DocumentId("doc-17"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-17"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = useCase.Execute(document);

        Assert.Same(document, result);
        Assert.True(result.HasExtractedText);
        Assert.Equal("Sample PDF text from page 1\nSample PDF text from page 2", result.ExtractedText?.Value);
        Assert.Equal(IngestionState.Accepted, result.State);
        Assert.Equal(ProcessingStage.PendingProcessing, result.ProcessingStage);
    }

    [Fact]
    public void ExtractTextWorkflow_RejectsDocumentWhenExtractionFails()
    {
        var useCase = new ExtractTextUseCase(new ThrowingTextExtractionService());
        var document = Document.Accept(
            new DocumentId("doc-18"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-18"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Extraction failed", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(IngestionOutcome.Failed, document.Outcome);
        Assert.Equal("Extraction failed", document.RejectionReason?.Value);
    }

    [Fact]
    public void NormalizeTextWorkflow_PersistsNormalizedTextAndPublishesEvent()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var normalizationService = new OcrTextNormalizationService();
        var normalizationUseCase = new NormalizeTextUseCase(normalizationService, null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-29"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-29"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("Hello   \t\tworld\r\n\r\nNext\nline"));
        repository.Save(document);

        normalizationUseCase.Execute(document);
        repository.Save(document);

        var persisted = repository.GetById("doc-29");

        Assert.True(persisted!.HasNormalizedText);
        Assert.Equal("Hello world\nNext line", persisted.NormalizedText?.Value);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "TextNormalized");
    }

    [Fact]
    public void NormalizeTextWorkflow_FailsDocumentAndPublishesFailureEventWhenNormalizationThrows()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var normalizationService = new ThrowingNormalizationService();
        var normalizationUseCase = new NormalizeTextUseCase(normalizationService, null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-30"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-30"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("hello world"));

        var ex = Assert.Throws<InvalidOperationException>(() => normalizationUseCase.Execute(document));

        Assert.Equal("Normalization failed", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(IngestionOutcome.Failed, document.Outcome);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "TextNormalizationFailed");
    }

    [Fact]
    public void ClauseDetectionWorkflow_PersistsClausesAndPublishesCompletionEvent()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var clauseDetectionService = new BoundaryClauseDetectionService();
        var useCase = new DetectClausesUseCase(clauseDetectionService, null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-35"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-35"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("1. First clause. 2. Second clause."));
        document.RecordNormalizedText(new NormalizedText("1. First clause. 2. Second clause."));
        repository.Save(document);

        var result = useCase.Execute(document);
        repository.Save(result);

        var persisted = repository.GetById("doc-35");

        Assert.Same(document, result);
        Assert.True(persisted!.HasClauses);
        Assert.Equal(2, persisted.Clauses.Count);
        Assert.Equal("First clause", persisted.Clauses[0].Text.Value);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "ClauseDetectionCompleted");
    }

    [Fact]
    public void ClauseDetectionWorkflow_UsesTenantScopedRepositoryLookup()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new DetectClausesUseCase(new BoundaryClauseDetectionService(), null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-36"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-36"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("1. One clause."));
        document.RecordNormalizedText(new NormalizedText("1. One clause."));
        repository.Save(document);

        useCase.Execute(document);

        Assert.NotNull(repository.GetByTenantAndIdempotencyKey("tenant-1", "tenant-1|upload|https://example.com/file.pdf"));
        Assert.Null(repository.GetByTenantAndIdempotencyKey("tenant-2", "tenant-1|upload|https://example.com/file.pdf"));
    }

    [Fact]
    public void ClassificationWorkflow_PersistsClassificationAndPublishesCompletionEvent()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new ClassifyDocumentUseCase(null, null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-37"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-37"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("This is a contract."));
        document.RecordNormalizedText(new NormalizedText("This is a contract."));
        document.RecordDetectedClauses([
            Clause.Create(
                new ClauseId("clause-1"),
                1,
                new ClauseText("This is a clause."),
                new ClauseSpan(0, 18))]);
        repository.Save(document);

        var result = useCase.Execute(document);
        repository.Save(result);

        var persisted = repository.GetById("doc-37");

        Assert.Same(document, result);
        Assert.True(persisted!.HasDocumentClassification);
        Assert.Equal("NDA", persisted.DocumentClassification!.ClassificationCode.Value);
        Assert.Equal(0.92m, persisted.DocumentClassification.ConfidenceScore.Value);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "DocumentClassificationCompleted");
    }

    [Fact]
    public void ClassificationWorkflow_FailsDocumentAndPublishesFailureEventWhenEvidenceMissing()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new ClassifyDocumentUseCase(null, null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-38"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-38"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Document classification failed", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "DocumentClassificationFailed");
    }

    [Fact]
    public void ClassificationWorkflow_IsDeterministicAcrossRepeatedExecution()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new ClassifyDocumentUseCase(null, null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-39"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-39"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("This is a contract."));
        document.RecordNormalizedText(new NormalizedText("This is a contract."));
        document.RecordDetectedClauses([
            Clause.Create(
                new ClauseId("clause-2"),
                1,
                new ClauseText("This is a clause."),
                new ClauseSpan(0, 18))]);
        repository.Save(document);

        var firstResult = useCase.Execute(document);
        repository.Save(firstResult);

        var secondResult = useCase.Execute(firstResult);

        Assert.Same(document, firstResult);
        Assert.Same(document, secondResult);
        Assert.True(document.HasDocumentClassification);
        Assert.Equal("NDA", document.DocumentClassification!.ClassificationCode.Value);
        Assert.Equal(3, document.Revisions.Count);
    }

    [Fact]
    public void SummaryWorkflow_PersistsSummaryAndPublishesCompletionEvent()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new GenerateDocumentSummaryUseCase(null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-40"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-40"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("This is a contract."));
        document.RecordDocumentClassification(DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(0.92m)));
        repository.Save(document);

        var result = useCase.Execute(document);
        repository.Save(result);

        var persisted = repository.GetById("doc-40");

        Assert.Same(document, result);
        Assert.True(persisted!.HasDocumentSummary);
        Assert.Equal("Summary for NDA: This is a contract.", persisted.DocumentSummary!.SummaryText.Value);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "DocumentSummaryCompleted");
    }

    [Fact]
    public void SummaryWorkflow_FailsDocumentAndPublishesFailureEventWhenEvidenceMissing()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new GenerateDocumentSummaryUseCase(null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-41"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-41"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Document summary failed", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "DocumentSummaryFailed");
    }

    [Fact]
    public void SummaryWorkflow_IsDeterministicAcrossRepeatedExecution()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new GenerateDocumentSummaryUseCase(null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-42"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-42"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("This is a contract."));
        document.RecordDocumentClassification(DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(0.92m)));
        repository.Save(document);

        var firstResult = useCase.Execute(document);
        repository.Save(firstResult);

        var secondResult = useCase.Execute(firstResult);

        Assert.Same(document, firstResult);
        Assert.Same(document, secondResult);
        Assert.True(document.HasDocumentSummary);
        Assert.Equal("Summary for NDA: This is a contract.", document.DocumentSummary!.SummaryText.Value);
        Assert.Equal(3, document.Revisions.Count);
    }

    [Fact]
    public void EmbeddingWorkflow_PersistsEmbeddingAndPublishesCompletionEvent()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new GenerateDocumentEmbeddingUseCase(null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-43"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-43"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("This is a contract."));
        document.RecordDocumentClassification(DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(0.92m)));
        repository.Save(document);

        var result = useCase.Execute(document);
        repository.Save(result);

        var persisted = repository.GetById("doc-43");

        Assert.Same(document, result);
        Assert.True(persisted!.HasDocumentEmbedding);
        Assert.Equal([5m, 3m, 2m, 3m, 4m], persisted.DocumentEmbedding!.EmbeddingVector.Values);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "DocumentEmbeddingCompleted");
    }

    [Fact]
    public void EmbeddingWorkflow_FailsDocumentAndPublishesFailureEventWhenEvidenceMissing()
    {
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new GenerateDocumentEmbeddingUseCase(null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-44"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-44"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var ex = Assert.Throws<InvalidOperationException>(() => useCase.Execute(document));

        Assert.Equal("Document embedding failed", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Equal(IngestionOutcome.Failed, document.Outcome);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "DocumentEmbeddingFailed");
    }

    private static IngestionRequest CreateValidRequest()
    {
        return new IngestionRequest(
            "tenant-1",
            "Upload",
            "PDF",
            2048,
            "application/pdf",
            "en",
            3,
            "Example Author",
            DateTimeOffset.Parse("2024-01-01T00:00:00Z"),
            "https://example.com/file.pdf",
            "corr-1",
            "tenant-1|upload|https://example.com/file.pdf");
    }

    private static IngestDocumentRequestContract CreateIngestRequestContract()
    {
        return new IngestDocumentRequestContract(
            "tenant-1",
            "Upload",
            "PDF",
            2048,
            "application/pdf",
            "en",
            3,
            "Alice",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "https://example.com/file.pdf",
            "corr-1",
            "tenant-1|upload|https://example.com/file.pdf");
    }

    private sealed class ThrowingTextExtractionService : ITextExtractionService
    {
        public TextExtractionResult Extract(string content, Document document)
        {
            throw new InvalidOperationException("boom");
        }
    }

    private sealed class ThrowingNormalizationService : ITextNormalizationService
    {
        public TextNormalizationResult Normalize(string content, Document document)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
