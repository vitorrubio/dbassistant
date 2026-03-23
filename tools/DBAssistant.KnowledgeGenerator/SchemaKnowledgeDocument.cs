namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Represents one generated RAG document written to the JSON artifact.
/// </summary>
public sealed class SchemaKnowledgeDocument
{
    /// <summary>
    /// Gets or sets the stable identifier of the document.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display title of the document.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the searchable content of the document.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the related table names covered by the document.
    /// </summary>
    public IReadOnlyCollection<string> TableNames { get; set; } = [];

    /// <summary>
    /// Gets or sets the normalized keywords extracted for retrieval.
    /// </summary>
    public IReadOnlyCollection<string> Keywords { get; set; } = [];
}
