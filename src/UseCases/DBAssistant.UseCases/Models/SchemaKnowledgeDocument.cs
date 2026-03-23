namespace DBAssistant.UseCases.Models;

/// <summary>
/// Represents one indexed schema knowledge document that can enrich prompt context before live metadata fallback.
/// </summary>
public sealed class SchemaKnowledgeDocument
{
    /// <summary>
    /// Gets or sets the human-readable title that identifies the indexed schema knowledge document.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the descriptive content stored in the schema knowledge index.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the related table names represented by the indexed document.
    /// </summary>
    public IReadOnlyCollection<string> TableNames { get; init; } = [];
}
