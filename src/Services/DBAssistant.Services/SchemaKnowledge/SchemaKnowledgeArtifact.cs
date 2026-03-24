using DBAssistant.UseCases.Models;

namespace DBAssistant.Services.SchemaKnowledge;

/// <summary>
/// Represents the serialized schema knowledge artifact consumed by the runtime retriever.
/// </summary>
public sealed class SchemaKnowledgeArtifact
{
    /// <summary>
    /// Gets or sets the logical database name represented by the artifact.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp at which the artifact was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the artifact format version.
    /// </summary>
    public string FormatVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated schema knowledge documents.
    /// </summary>
    public IReadOnlyCollection<SchemaKnowledgeDocument> Documents { get; set; } = [];
}
