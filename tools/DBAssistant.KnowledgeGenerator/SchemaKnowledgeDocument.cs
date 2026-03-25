namespace DBAssistant.KnowledgeGenerator;

using System.Text.Json.Serialization;

/// <summary>
/// Represents one generated RAG document written to the JSON artifact.
/// </summary>
public sealed class SchemaKnowledgeDocument
{
    /// <summary>
    /// Gets or sets the stable identifier of the document.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document type.
    /// </summary>
    [JsonPropertyName("doc_type")]
    public string DocType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    [JsonPropertyName("database_name")]
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    [JsonPropertyName("schema_name")]
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary table name represented by the document.
    /// </summary>
    [JsonPropertyName("table_name")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the related table names covered by the document.
    /// </summary>
    [JsonPropertyName("related_tables")]
    public IReadOnlyCollection<string> RelatedTables { get; set; } = [];

    /// <summary>
    /// Gets or sets the display title of the document.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the searchable human-readable content of the document.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized keywords extracted for retrieval.
    /// </summary>
    [JsonPropertyName("keywords")]
    public IReadOnlyCollection<string> Keywords { get; set; } = [];

    /// <summary>
    /// Gets or sets explicit join hints associated with the document.
    /// </summary>
    [JsonPropertyName("join_hints")]
    public IReadOnlyCollection<string> JoinHints { get; set; } = [];

    /// <summary>
    /// Gets or sets example user questions that the document helps answer.
    /// </summary>
    [JsonPropertyName("question_patterns")]
    public IReadOnlyCollection<string> QuestionPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets semantic tags useful for retrieval and SQL planning.
    /// </summary>
    [JsonPropertyName("semantic_tags")]
    public IReadOnlyCollection<string> SemanticTags { get; set; } = [];

    /// <summary>
    /// Gets or sets the metadata source used to build the document.
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the extraction timestamp.
    /// </summary>
    [JsonPropertyName("last_extracted_at_utc")]
    public DateTimeOffset LastExtractedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the hash of the retrieval-relevant content.
    /// </summary>
    [JsonPropertyName("content_hash")]
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the embedding-optimized text.
    /// </summary>
    [JsonPropertyName("embedding_input")]
    public string EmbeddingInput { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated token count of the embedding input.
    /// </summary>
    [JsonPropertyName("token_estimate")]
    public int TokenEstimate { get; set; }
}
