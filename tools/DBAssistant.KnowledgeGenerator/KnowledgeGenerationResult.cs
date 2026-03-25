namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Represents the generated runtime artifact paths.
/// </summary>
public sealed class KnowledgeGenerationResult
{
    /// <summary>
    /// Gets or sets the schema documents JSON path.
    /// </summary>
    public string SchemaDocumentsPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the embedding input JSONL path.
    /// </summary>
    public string EmbeddingInputPath { get; set; } = string.Empty;
}
