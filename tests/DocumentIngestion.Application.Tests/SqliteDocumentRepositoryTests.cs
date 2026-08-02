using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Application.Tests;

public sealed class SqliteDocumentRepositoryTests
{
    [Fact]
    public void SaveAndReload_PersistsDocumentAndRevisionHistory()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "document-ingestion-tests", Guid.NewGuid().ToString("N"), "documents.sqlite");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        var connectionString = $"Data Source={databasePath};Mode=ReadWriteCreate";
        var repository = new SqliteDocumentRepository(connectionString);

        var document = Document.Accept(
            new DocumentId("doc-sqlite"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en", 3, "Alice", DateTimeOffset.Parse("2024-01-01T00:00:00Z")),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-sqlite"),
            new IdempotencyKey("tenant-1|upload|doc-sqlite"));

        document.RecordDetectedDocumentType(new DocumentType("Pdf"));
        document.RecordExtractedText(new RawText("raw text"));
        document.RecordNormalizedText(new NormalizedText("normalized text"));

        repository.Save(document);

        var reloadedRepository = new SqliteDocumentRepository(connectionString);
        var persisted = reloadedRepository.GetById(document.Id.Value);

        Assert.NotNull(persisted);
        Assert.Equal(document.Id.Value, persisted!.Id.Value);
        Assert.Equal(document.TenantId.Value, persisted.TenantId.Value);
        Assert.Equal(document.State, persisted.State);
        Assert.Equal(document.ProcessingStage, persisted.ProcessingStage);
        Assert.Equal(document.Outcome, persisted.Outcome);
        Assert.Equal(document.DetectedDocumentType?.Value, persisted.DetectedDocumentType?.Value);
        Assert.Equal(document.ExtractedText?.Value, persisted.ExtractedText?.Value);
        Assert.Equal(document.NormalizedText?.Value, persisted.NormalizedText?.Value);
        Assert.Equal(document.Revisions.Count, persisted.Revisions.Count);
    }

    [Fact]
    public void SaveAndReload_PersistsProcessingArtifactsAndAuditEvents()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), "document-ingestion-tests", Guid.NewGuid().ToString("N"), "documents-artifacts.sqlite");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        var connectionString = $"Data Source={databasePath};Mode=ReadWriteCreate";
        var repository = new SqliteDocumentRepository(connectionString);

        var document = Document.Accept(
            new DocumentId("doc-artifacts"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en", 3, "Alice", DateTimeOffset.Parse("2024-01-01T00:00:00Z")),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-artifacts"),
            new IdempotencyKey("tenant-1|upload|doc-artifacts"));

        document.RecordDetectedDocumentType(new DocumentType("Pdf"));
        document.RecordExtractedText(new RawText("raw text"));
        document.RecordNormalizedText(new NormalizedText("normalized text"));
        document.RecordDetectedClauses(new[]
        {
            Clause.Create(
                ClauseId.CreateDeterministic("doc-artifacts", 1, 1),
                1,
                new ClauseText("The parties will comply."),
                new ClauseSpan(0, 24),
                new ClauseNumberLabel("1"))
        });
        document.RecordCategoryAssignments(new[]
        {
            ClauseCategoryAssignment.Create(
                ClauseId.CreateDeterministic("doc-artifacts", 1, 1),
                new ClauseCategoryCode("Obligation"),
                new ConfidenceScore(0.95m))
        });
        document.RecordDocumentClassification(DocumentClassificationResult.Create(
            new DocumentClassificationCode("NDA"),
            new ConfidenceScore(0.92m)));
        document.RecordDocumentSummary(DocumentSummaryResult.Create(
            new SummaryText("The document outlines an obligation.")));
        document.RecordDocumentEmbedding(DocumentEmbeddingResult.Create(
            new EmbeddingVector(new List<decimal> { 0.1m, 0.2m, 0.3m })));

        repository.Save(document);

        var reloadedRepository = new SqliteDocumentRepository(connectionString);
        var persisted = reloadedRepository.GetById(document.Id.Value);

        Assert.NotNull(persisted);
        Assert.True(persisted!.HasClauses);
        Assert.True(persisted.HasCategoryAssignments);
        Assert.True(persisted.HasDocumentClassification);
        Assert.True(persisted.HasDocumentSummary);
        Assert.True(persisted.HasDocumentEmbedding);
        Assert.Equal("Obligation", persisted.CategoryAssignments[0].CategoryCode.Value);
        Assert.Equal("NDA", persisted.DocumentClassification!.ClassificationCode.Value);
        Assert.Equal("The document outlines an obligation.", persisted.DocumentSummary!.SummaryText.Value);
        Assert.Equal(0.3m, persisted.DocumentEmbedding!.EmbeddingVector.Values[2]);
    }

    [Fact]
    public void SchemaDefinition_ContainsCoreTablesAndIndexes()
    {
        Assert.Contains("CREATE TABLE IF NOT EXISTS documents", SqliteSchemaScript.CreateTablesSql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS document_revisions", SqliteSchemaScript.CreateTablesSql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS processing_events", SqliteSchemaScript.CreateTablesSql);
        Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS idx_documents_tenant_idempotency", SqliteSchemaScript.CreateTablesSql);
    }
}
