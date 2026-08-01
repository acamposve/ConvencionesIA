using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class DocumentIngestionAcceptanceTests
{
    [Fact]
    public void ValidIngestion_IsAcceptedAndTraceable()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = new IngestDocumentRequestContract(
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

        var response = endpoint.Handle(request, userId: "user-1", callerTenantId: "tenant-1");
        var persisted = repository.GetById(response.DocumentId);

        Assert.Equal("Accepted", response.Outcome);
        Assert.Equal("PendingProcessing", response.ProcessingStage);
        Assert.Equal("corr-1", response.CorrelationId);
        Assert.Equal("v1", response.Version);
        Assert.NotNull(persisted);
        Assert.Equal(IngestionState.Accepted, persisted!.State);
        Assert.Equal(ProcessingStage.PendingProcessing, persisted.ProcessingStage);
        Assert.Equal("https://example.com/file.pdf", persisted.Provenance.SourceReference);
        Assert.Equal("corr-1", persisted.CorrelationId.Value);
        Assert.Single(publisher.AuditRecords);
        Assert.Equal("DocumentIngestionCompleted", publisher.AuditRecords[0].EventName);
    }

    [Fact]
    public void UnsupportedSource_IsRejectedWithBusinessReason()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = new IngestDocumentRequestContract(
            "tenant-1",
            "Email",
            "PDF",
            2048,
            "application/pdf",
            "en",
            3,
            "Alice",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "https://example.com/file.pdf",
            "corr-2",
            "tenant-1|email|https://example.com/file.pdf");

        var response = endpoint.Handle(request, userId: "user-1", callerTenantId: "tenant-1");
        var persisted = repository.GetById(response.DocumentId);

        Assert.Equal("Rejected", response.Outcome);
        Assert.Equal("Unsupported source", response.RejectionReason);
        Assert.Equal("None", response.ProcessingStage);
        Assert.NotNull(persisted);
        Assert.Equal(IngestionState.Rejected, persisted!.State);
        Assert.Equal(ProcessingStage.None, persisted.ProcessingStage);
        Assert.Empty(publisher.AuditRecords);
    }

    [Fact]
    public void UnsupportedFormat_IsRejectedWithBusinessReason()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = new IngestDocumentRequestContract(
            "tenant-1",
            "Upload",
            "TXT",
            2048,
            "text/plain",
            "en",
            3,
            "Alice",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "https://example.com/file.txt",
            "corr-3",
            "tenant-1|upload|https://example.com/file.txt");

        var response = endpoint.Handle(request, userId: "user-1", callerTenantId: "tenant-1");
        var persisted = repository.GetById(response.DocumentId);

        Assert.Equal("Rejected", response.Outcome);
        Assert.Equal("Unsupported format", response.RejectionReason);
        Assert.Equal("None", response.ProcessingStage);
        Assert.NotNull(persisted);
        Assert.Equal(IngestionState.Rejected, persisted!.State);
        Assert.Equal(ProcessingStage.None, persisted.ProcessingStage);
        Assert.Empty(publisher.AuditRecords);
    }

    [Fact]
    public void MissingTenantContext_IsRejectedBeforeProcessingStarts()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);
        var endpoint = new DocumentIngestionEndpoint(useCase);

        var request = new IngestDocumentRequestContract(
            " ",
            "Upload",
            "PDF",
            2048,
            "application/pdf",
            "en",
            3,
            "Alice",
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "https://example.com/file.pdf",
            "corr-4",
            "tenant-1|upload|https://example.com/file.pdf");

        var ex = Assert.Throws<ArgumentException>(() => endpoint.Handle(request, userId: "user-1", callerTenantId: " "));

        Assert.Contains("TenantId", ex.Message);
        Assert.Empty(publisher.AuditRecords);
        Assert.Null(repository.GetByTenantAndIdempotencyKey("tenant-1", "tenant-1|upload|https://example.com/file.pdf"));
    }

    [Fact]
    public void ClauseDetectionWorkflow_ProducesOrderedClausesForNormalizedText()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var useCase = new DetectClausesUseCase(new BoundaryClauseDetectionService(), null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-40"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-40"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("1. First clause. 2. Second clause."));
        document.RecordNormalizedText(new NormalizedText("1. First clause. 2. Second clause."));
        repository.Save(document);

        useCase.Execute(document);
        repository.Save(document);

        var persisted = repository.GetById("doc-40");

        Assert.NotNull(persisted);
        Assert.Equal(2, persisted!.Clauses.Count);
        Assert.Equal("First clause", persisted.Clauses[0].Text.Value);
        Assert.Equal("Second clause", persisted.Clauses[1].Text.Value);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "ClauseDetectionCompleted");
    }

    [Fact]
    public void ClauseCategorizationWorkflow_PersistsClausesAndAssignmentsAcrossRepositoryRoundTrip()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var normalizationUseCase = new NormalizeTextUseCase(new OcrTextNormalizationService());
        var detectionUseCase = new DetectClausesUseCase(new BoundaryClauseDetectionService(), null, publisher);
        var categorizationUseCase = new CategorizeClausesUseCase(new BoundaryClauseCategorizationService(), null, publisher);
        var extractionUseCase = new ExtractTextUseCase(
            new TextExtractionServiceRouter(new StubTextExtractionService("1. First clause. 2. Second clause.", "Pdf"), new StubTextExtractionService("1. First clause. 2. Second clause.", "Pdf"), new StubTextExtractionService("1. First clause. 2. Second clause.", "Pdf")),
            null,
            publisher,
            normalizationUseCase,
            detectionUseCase,
            categorizationUseCase,
            null);
        var document = Document.Accept(
            new DocumentId("doc-41"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-41"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        var result = extractionUseCase.Execute(document);
        repository.Save(result);

        var persisted = repository.GetById("doc-41");

        Assert.NotNull(persisted);
        Assert.True(persisted!.HasClauses);
        Assert.True(persisted.HasCategoryAssignments);
        Assert.Equal(2, persisted.Clauses.Count);
        Assert.Equal("Obligation", persisted.CategoryAssignments[0].CategoryCode.Value);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "ClauseDetectionCompleted");
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "ClauseCategorizationCompleted");
    }

    [Fact]
    public void DocumentClassificationWorkflow_AssignsClassificationWhenEvidenceIsPresent()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var classificationUseCase = new ClassifyDocumentUseCase(null, null, publisher);
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
        document.RecordNormalizedText(new NormalizedText("This is a contract."));
        document.RecordDetectedClauses([
            Clause.Create(
                new ClauseId("clause-42"),
                1,
                new ClauseText("This is a clause."),
                new ClauseSpan(0, 18))]);
        repository.Save(document);

        var result = classificationUseCase.Execute(document);
        repository.Save(result);

        var persisted = repository.GetById("doc-42");

        Assert.Same(document, result);
        Assert.NotNull(persisted);
        Assert.True(persisted!.HasDocumentClassification);
        Assert.Equal("NDA", persisted.DocumentClassification!.ClassificationCode.Value);
        Assert.Equal(0.92m, persisted.DocumentClassification.ConfidenceScore.Value);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "DocumentClassificationCompleted");
    }

    [Fact]
    public void DocumentClassificationWorkflow_FailsAndPersistsFailureWhenEvidenceIsMissing()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var classificationUseCase = new ClassifyDocumentUseCase(null, null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-43"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-43"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        repository.Save(document);

        var ex = Assert.Throws<InvalidOperationException>(() => classificationUseCase.Execute(document));

        Assert.Equal("Document classification failed", ex.Message);
        Assert.Equal(IngestionState.Failed, document.State);
        Assert.Contains(publisher.AuditRecords, record => record.EventName == "DocumentClassificationFailed");
        Assert.NotNull(repository.GetById("doc-43"));
    }

    [Fact]
    public void DocumentClassificationWorkflow_IsDeterministicAcrossReruns()
    {
        var repository = new InMemoryDocumentRepository();
        var publisher = new DocumentIngestionEventPublisher();
        var classificationUseCase = new ClassifyDocumentUseCase(null, null, publisher);
        var document = Document.Accept(
            new DocumentId("doc-44"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-44"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        document.RecordExtractedText(new RawText("This is a contract."));
        document.RecordNormalizedText(new NormalizedText("This is a contract."));
        document.RecordDetectedClauses([
            Clause.Create(
                new ClauseId("clause-44"),
                1,
                new ClauseText("This is a clause."),
                new ClauseSpan(0, 18))]);
        repository.Save(document);

        var firstResult = classificationUseCase.Execute(document);
        repository.Save(firstResult);
        var secondResult = classificationUseCase.Execute(firstResult);

        Assert.Same(document, firstResult);
        Assert.Same(document, secondResult);
        Assert.Equal("NDA", document.DocumentClassification!.ClassificationCode.Value);
        Assert.Equal(3, document.Revisions.Count);
    }

    private sealed class StubTextExtractionService : ITextExtractionService
    {
        private readonly string _text;
        private readonly string _strategy;

        public StubTextExtractionService(string text, string strategy)
        {
            _text = text;
            _strategy = strategy;
        }

        public TextExtractionResult Extract(string content, Document document)
        {
            return new TextExtractionResult(_text, _strategy);
        }
    }
}
