using DocumentIngestion.Application;
using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public class IngestDocumentUseCaseTests
{
    [Fact]
    public void Execute_PersistsAndPublishesAcceptedDocument()
    {
        var repository = new TestDocumentRepository();
        var publisher = new TestIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);

        var request = new IngestionRequest(
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

        var result = useCase.Execute(request);

        Assert.Equal("Accepted", result.State);
        Assert.Equal("PendingProcessing", result.ProcessingStage);
        Assert.Null(result.RejectionReason);
        Assert.NotNull(repository.SavedDocument);
        Assert.NotNull(publisher.PublishedDocument);
    }

    [Fact]
    public void Execute_PersistsRejectedDocumentWhenDomainValidationFails()
    {
        var repository = new TestDocumentRepository();
        var publisher = new TestIngestionEventPublisher();
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
            "corr-1",
            "tenant-1|upload|https://example.com/file.pdf");

        var result = useCase.Execute(request);

        Assert.Equal("Rejected", result.State);
        Assert.Equal("None", result.ProcessingStage);
        Assert.Equal("Unsupported source", result.RejectionReason);
        Assert.NotNull(repository.SavedDocument);
        Assert.Null(publisher.PublishedDocument);
    }

    [Fact]
    public void Execute_UsesRepositoryAtomicCreateWhenIdempotencyKeyAlreadyExists()
    {
        var repository = new TestDocumentRepository();
        var publisher = new TestIngestionEventPublisher();
        var useCase = new IngestDocumentUseCase(repository, publisher);

        var request = new IngestionRequest(
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

        var first = useCase.Execute(request);
        var second = useCase.Execute(request);

        Assert.Equal(first.DocumentId, second.DocumentId);
        Assert.Equal(1, repository.SaveAttempts);
        Assert.NotNull(repository.SavedDocument);
    }

    private sealed class TestDocumentRepository : IDocumentRepository

    {
        private readonly Dictionary<string, Document> _documentsByIdempotencyKey = new(StringComparer.OrdinalIgnoreCase);

        public Document? SavedDocument { get; private set; }
        public int SaveAttempts { get; private set; }

        public void Save(Document document)
        {
            SaveAttempts++;
            SavedDocument = document;
            _documentsByIdempotencyKey[BuildCompositeKey(document.TenantId.Value, document.IdempotencyKey.Value)] = document;
        }

        public Document? GetById(string id) => null;

        public Document? GetByTenantAndIdempotencyKey(string tenantId, string idempotencyKey)
        {
            var compositeKey = BuildCompositeKey(tenantId, idempotencyKey);
            return _documentsByIdempotencyKey.TryGetValue(compositeKey, out var document) ? document : null;
        }

        public bool TryCreate(Document document)
        {
            if (GetByTenantAndIdempotencyKey(document.TenantId.Value, document.IdempotencyKey.Value) is not null)
            {
                return false;
            }

            Save(document);
            return true;
        }

        private static string BuildCompositeKey(string tenantId, string idempotencyKey)
        {
            return $"{tenantId}:{idempotencyKey}";
        }
    }

    private sealed class TestIngestionEventPublisher : IIngestionEventPublisher
    {
        public Document? PublishedDocument { get; private set; }

        public void Publish(Document document) => PublishedDocument = document;

        public void PublishTextExtracted(Document document, string extractionStrategy, int textLength)
        {
        }

        public void PublishTextExtractionFailed(Document document, string reason)
        {
        }

        public void PublishTextNormalized(Document document, string normalizationStrategy, int textLength)
        {
        }

        public void PublishTextNormalizationFailed(Document document, string reason)
        {
        }

        public void PublishClauseDetectionCompleted(Document document, int clauseCount)
        {
        }

        public void PublishClauseDetectionFailed(Document document, string reason)
        {
        }

        public void PublishClauseCategorizationCompleted(Document document, int clauseCount)
        {
        }

        public void PublishClauseCategorizationFailed(Document document, string reason)
        {
        }

        public void PublishDocumentClassificationCompleted(Document document, string classificationCode, decimal confidenceScore)
        {
        }

        public void PublishDocumentClassificationFailed(Document document, string reason)
        {
        }

        public void PublishDocumentSummaryCompleted(Document document, string summaryText)
        {
        }

        public void PublishDocumentSummaryFailed(Document document, string reason)
        {
        }
    }

}
