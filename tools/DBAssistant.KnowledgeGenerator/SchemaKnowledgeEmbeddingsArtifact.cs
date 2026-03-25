namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Represents the serialized embeddings generated for schema knowledge documents.
/// </summary>
public sealed class SchemaKnowledgeEmbeddingsArtifact
{
    /// <summary>
    /// Gets or sets the source format version.
    /// </summary>
    public string FormatVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp of the source knowledge artifact generation.
    /// </summary>
    public DateTimeOffset KnowledgeGeneratedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the embedding model used to generate the vectors.
    /// </summary>
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the embedded documents.
    /// </summary>
    public IReadOnlyCollection<SchemaKnowledgeEmbeddingDocument> Documents { get; set; } = [];
}

/// <summary>
/// Represents one embedded schema knowledge document.
/// </summary>
public sealed class SchemaKnowledgeEmbeddingDocument
{
    /// <summary>
    /// Gets or sets the source document identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the embedding vector.
    /// </summary>
    public IReadOnlyCollection<float> Embedding { get; set; } = [];
}
