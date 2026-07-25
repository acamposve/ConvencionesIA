using DocumentIngestion.Domain;
using Xunit;

namespace DocumentIngestion.Domain.Tests;

public class DocumentIngestionServiceTests
{
    [Fact]
    public void EvaluateAcceptance_AcceptsValidRequest()
    {
        var service = new DocumentIngestionService();

        var document = service.EvaluateAcceptance(
            new DocumentId("doc-10"),
            new TenantId("tenant-1"),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-10"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"));

        Assert.Equal(IngestionState.Accepted, document.State);
        Assert.Equal(ProcessingStage.PendingProcessing, document.ProcessingStage);
    }

    [Fact]
    public void EvaluateRejection_RejectsForMissingTenantContext()
    {
        var service = new DocumentIngestionService();

        var ex = Assert.Throws<DomainValidationException>(() => service.EvaluateRejection(
            new DocumentId("doc-11"),
            new TenantId("   "),
            new DocumentSource("Upload"),
            new DocumentFormat("PDF"),
            new DocumentMetadata(2048, "application/pdf", "en"),
            new Provenance("https://example.com/file.pdf", "Example"),
            new CorrelationId("corr-11"),
            new IdempotencyKey("tenant-1|upload|https://example.com/file.pdf"),
            new RejectionReason("Unsupported format")));

        Assert.Equal("Invalid tenant context", ex.Message);
    }
}
