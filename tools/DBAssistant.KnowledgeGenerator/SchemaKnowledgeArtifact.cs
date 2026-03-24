namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Represents the generated RAG artifact written to disk.
/// </summary>
public sealed class SchemaKnowledgeArtifact
{
    /// <summary>
    /// Gets or sets the logical name of the source database.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp at which the artifact was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the artifact format version.
    /// </summary>
    public string FormatVersion { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the generated RAG documents.
    /// </summary>
    public IReadOnlyCollection<SchemaKnowledgeDocument> Documents { get; set; } = [];
}
