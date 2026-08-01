namespace DocumentIngestion.Domain;

public sealed class DocumentEmbeddingResult
{
    private DocumentEmbeddingResult(EmbeddingVector embeddingVector, EmbeddingStatus status)
    {
        EmbeddingVector = embeddingVector;
        Status = status;
    }

    public EmbeddingVector EmbeddingVector { get; }
    public EmbeddingStatus Status { get; }

    public static DocumentEmbeddingResult Create(EmbeddingVector embeddingVector, EmbeddingStatus status = EmbeddingStatus.Completed)
    {
        ArgumentNullException.ThrowIfNull(embeddingVector);

        if (embeddingVector.Values.Count == 0)
        {
            throw new DomainValidationException("Embedding vector is required.");
        }

        return new DocumentEmbeddingResult(embeddingVector, status);
    }
}

public sealed record EmbeddingVector(IReadOnlyList<decimal> Values)
{
    public override string ToString() => string.Join(",", Values);
}

public enum EmbeddingStatus
{
    Completed,
    Failed
}
