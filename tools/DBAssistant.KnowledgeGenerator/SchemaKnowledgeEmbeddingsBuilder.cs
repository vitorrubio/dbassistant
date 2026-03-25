namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Builds the runtime schema embeddings artifact from generated schema documents.
/// </summary>
public sealed class SchemaKnowledgeEmbeddingsBuilder
{
    private readonly IEmbeddingVectorProvider _vectorProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaKnowledgeEmbeddingsBuilder"/> class.
    /// </summary>
    public SchemaKnowledgeEmbeddingsBuilder()
        : this(new OpenAiEmbeddingVectorProvider())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaKnowledgeEmbeddingsBuilder"/> class with a custom vector provider.
    /// </summary>
    /// <param name="vectorProvider">The provider that produces embedding vectors.</param>
    public SchemaKnowledgeEmbeddingsBuilder(IEmbeddingVectorProvider vectorProvider)
    {
        _vectorProvider = vectorProvider;
    }

    /// <summary>
    /// Creates the schema embeddings artifact for the provided schema knowledge corpus.
    /// </summary>
    /// <param name="options">The generation options that contain the embedding configuration.</param>
    /// <param name="artifact">The schema knowledge artifact used as the embedding source.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the operation.</param>
    /// <returns>The generated schema embeddings artifact.</returns>
    public async Task<SchemaKnowledgeEmbeddingsArtifact> BuildAsync(
        KnowledgeGenerationOptions options,
        SchemaKnowledgeArtifact artifact,
        CancellationToken cancellationToken)
    {
        var orderedDocuments = artifact.Documents.ToArray();
        var vectors = await _vectorProvider.CreateEmbeddingsAsync(
            options,
            orderedDocuments.Select(document => document.EmbeddingInput).ToArray(),
            cancellationToken);

        return new SchemaKnowledgeEmbeddingsArtifact
        {
            FormatVersion = artifact.FormatVersion,
            KnowledgeGeneratedAtUtc = artifact.GeneratedAtUtc,
            EmbeddingModel = options.OpenAiEmbeddingModel,
            Documents = orderedDocuments
                .Zip(vectors, static (document, vector) => new SchemaKnowledgeEmbeddingDocument
                {
                    Id = document.Id,
                    Embedding = vector
                })
                .ToArray()
        };
    }
}
