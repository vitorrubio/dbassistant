namespace DBAssistant.UseCases.Models;

/// <summary>
/// Represents the assembled schema context together with the metadata that identifies its origin.
/// </summary>
public sealed class SchemaContextEnvelope
{
    /// <summary>
    /// Gets or sets the schema context text that will be injected into the language-model prompt.
    /// </summary>
    public string Context { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier that describes which schema sources were used to build the context.
    /// </summary>
    public string Source { get; init; } = string.Empty;
}
