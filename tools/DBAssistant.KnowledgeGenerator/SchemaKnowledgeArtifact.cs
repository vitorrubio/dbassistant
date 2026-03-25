namespace DBAssistant.KnowledgeGenerator;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the generated RAG artifact written to disk.
/// </summary>
public sealed class SchemaKnowledgeArtifact
{
    /// <summary>
    /// Gets or sets the logical name of the source database.
    /// </summary>
    [JsonPropertyName("database_name")]
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name represented by the artifact.
    /// </summary>
    [JsonPropertyName("schema_name")]
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp at which the artifact was generated.
    /// </summary>
    [JsonPropertyName("generated_at_utc")]
    public DateTimeOffset GeneratedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the artifact format version.
    /// </summary>
    [JsonPropertyName("format_version")]
    public string FormatVersion { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets the number of generated documents.
    /// </summary>
    [JsonPropertyName("document_count")]
    public int DocumentCount { get; set; }

    /// <summary>
    /// Gets or sets the generated RAG documents.
    /// </summary>
    [JsonPropertyName("documents")]
    public IReadOnlyCollection<SchemaKnowledgeDocument> Documents { get; set; } = [];
}
