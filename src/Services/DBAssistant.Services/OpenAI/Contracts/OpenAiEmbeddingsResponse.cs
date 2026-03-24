using System.Text.Json.Serialization;

namespace DBAssistant.Services.OpenAI.Contracts;

/// <summary>
/// Represents the response returned by the OpenAI embeddings endpoint.
/// </summary>
public sealed class OpenAiEmbeddingsResponse
{
    /// <summary>
    /// Gets or sets the embedding result items.
    /// </summary>
    public IReadOnlyCollection<OpenAiEmbeddingItem> Data { get; init; } = [];
}

/// <summary>
/// Represents one embedding vector returned by the API.
/// </summary>
public sealed class OpenAiEmbeddingItem
{
    /// <summary>
    /// Gets or sets the embedding vector values.
    /// </summary>
    [JsonPropertyName("embedding")]
    public IReadOnlyCollection<float> Embedding { get; init; } = [];
}
