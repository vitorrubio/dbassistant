namespace DBAssistant.KnowledgeGenerator;

using System.Text.Json.Serialization;

/// <summary>
/// Represents one JSONL line used as the input source for embeddings generation.
/// </summary>
public sealed class SchemaEmbeddingInputRecord
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("doc_type")]
    public string DocType { get; set; } = string.Empty;

    [JsonPropertyName("database_name")]
    public string DatabaseName { get; set; } = string.Empty;

    [JsonPropertyName("schema_name")]
    public string SchemaName { get; set; } = string.Empty;

    [JsonPropertyName("table_name")]
    public string TableName { get; set; } = string.Empty;

    [JsonPropertyName("related_tables")]
    public IReadOnlyCollection<string> RelatedTables { get; set; } = [];

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("embedding_input")]
    public string EmbeddingInput { get; set; } = string.Empty;

    [JsonPropertyName("content_hash")]
    public string ContentHash { get; set; } = string.Empty;

    [JsonPropertyName("token_estimate")]
    public int TokenEstimate { get; set; }
}
