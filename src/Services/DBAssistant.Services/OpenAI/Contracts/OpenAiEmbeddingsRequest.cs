namespace DBAssistant.Services.OpenAI.Contracts;

/// <summary>
/// Represents one request sent to the OpenAI embeddings endpoint.
/// </summary>
public sealed class OpenAiEmbeddingsRequest
{
    /// <summary>
    /// Gets or sets the embedding model identifier.
    /// </summary>
    public string Model { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the input text to embed.
    /// </summary>
    public string Input { get; init; } = string.Empty;
}
