namespace DBAssistant.UseCases.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents one indexed schema knowledge document that can enrich prompt context before live metadata fallback.
/// </summary>
public sealed class SchemaKnowledgeDocument
{
    /// <summary>
    /// Gets or sets the stable document identifier stored in the knowledge artifact.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the document type.
    /// </summary>
    [JsonPropertyName("doc_type")]
    public string DocType { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name associated with the document.
    /// </summary>
    [JsonPropertyName("database_name")]
    public string DatabaseName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name associated with the document.
    /// </summary>
    [JsonPropertyName("schema_name")]
    public string SchemaName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary table name represented by the document.
    /// </summary>
    [JsonPropertyName("table_name")]
    public string TableName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the related table names represented by the indexed document.
    /// </summary>
    [JsonPropertyName("related_tables")]
    public IReadOnlyCollection<string> RelatedTables { get; init; } = [];

    /// <summary>
    /// Gets or sets the human-readable title that identifies the indexed schema knowledge document.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the descriptive content stored in the schema knowledge index.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized keywords extracted from the document for retrieval scoring.
    /// </summary>
    [JsonPropertyName("keywords")]
    public IReadOnlyCollection<string> Keywords { get; init; } = [];

    /// <summary>
    /// Gets or sets explicit join hints helpful for SQL planning.
    /// </summary>
    [JsonPropertyName("join_hints")]
    public IReadOnlyCollection<string> JoinHints { get; init; } = [];

    /// <summary>
    /// Gets or sets example user questions that the document helps answer.
    /// </summary>
    [JsonPropertyName("question_patterns")]
    public IReadOnlyCollection<string> QuestionPatterns { get; init; } = [];

    /// <summary>
    /// Gets or sets semantic tags useful for hybrid retrieval.
    /// </summary>
    [JsonPropertyName("semantic_tags")]
    public IReadOnlyCollection<string> SemanticTags { get; init; } = [];

    /// <summary>
    /// Gets or sets the source metadata used to build the document.
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the extraction timestamp of the document.
    /// </summary>
    [JsonPropertyName("last_extracted_at_utc")]
    public DateTimeOffset LastExtractedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets the hash of the retrieval-relevant content.
    /// </summary>
    [JsonPropertyName("content_hash")]
    public string ContentHash { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the embedding-oriented text of the document.
    /// </summary>
    [JsonPropertyName("embedding_input")]
    public string EmbeddingInput { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated token count of the embedding input.
    /// </summary>
    [JsonPropertyName("token_estimate")]
    public int TokenEstimate { get; init; }

    /// <summary>
    /// Gets or sets the legacy table-name list used by the earlier artifact shape.
    /// </summary>
    [JsonPropertyName("table_names")]
    public IReadOnlyCollection<string> LegacyTableNames { get; init; } = [];

    /// <summary>
    /// Returns all table names associated with the document across current and legacy fields.
    /// </summary>
    public IReadOnlyCollection<string> GetAllTableNames()
    {
        return RelatedTables
            .Append(TableName)
            .Concat(LegacyTableNames)
            .Where(value => string.IsNullOrWhiteSpace(value) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
