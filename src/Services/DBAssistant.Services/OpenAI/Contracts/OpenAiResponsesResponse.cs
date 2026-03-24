using System.Text.Json.Serialization;

namespace DBAssistant.Services.OpenAI.Contracts;

/// <summary>
/// Represents the typed envelope returned by the OpenAI Responses API.
/// </summary>
public sealed class OpenAiResponsesResponse
{
    /// <summary>
    /// Gets or sets the optional direct output text.
    /// </summary>
    [JsonPropertyName("output_text")]
    public string? OutputText { get; init; }

    /// <summary>
    /// Gets or sets the structured output items.
    /// </summary>
    public IReadOnlyCollection<OpenAiResponseOutputItem> Output { get; init; } = [];
}

/// <summary>
/// Represents one output item inside the response envelope.
/// </summary>
public sealed class OpenAiResponseOutputItem
{
    public string? Type { get; init; }

    public string? Name { get; init; }

    public string? Arguments { get; init; }

    public IReadOnlyCollection<OpenAiResponseContentItem> Content { get; init; } = [];
}

/// <summary>
/// Represents one content item inside an output message.
/// </summary>
public sealed class OpenAiResponseContentItem
{
    public string? Type { get; init; }

    public string? Text { get; init; }
}
