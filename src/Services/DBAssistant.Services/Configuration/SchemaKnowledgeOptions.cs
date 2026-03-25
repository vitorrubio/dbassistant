namespace DBAssistant.Services.Configuration;

/// <summary>
/// Stores the configuration for the indexed schema knowledge source used before live metadata fallback.
/// </summary>
public sealed class SchemaKnowledgeOptions
{
    /// <summary>
    /// Gets or sets the file path of the JSON knowledge index used by the bootstrap retrieval implementation.
    /// </summary>
    public string FilePath { get; set; } = "knowledge/runtime/schema-documents.json";

    /// <summary>
    /// Gets or sets the maximum number of schema knowledge documents returned for one query.
    /// </summary>
    public int MaxDocuments { get; set; } = 3;

    /// <summary>
    /// Gets or sets the file path where generated schema-document embeddings are stored.
    /// </summary>
    public string EmbeddingsFilePath { get; set; } = "knowledge/runtime/schema-embeddings.json";
}
