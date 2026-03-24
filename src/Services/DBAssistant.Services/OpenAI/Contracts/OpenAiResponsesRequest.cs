using System.Text.Json.Serialization;

namespace DBAssistant.Services.OpenAI.Contracts;

/// <summary>
/// Represents one request sent to the OpenAI Responses API.
/// </summary>
public sealed class OpenAiResponsesRequest
{
    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string Model { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional system instructions.
    /// </summary>
    public string? Instructions { get; init; }

    /// <summary>
    /// Gets or sets the optional text-format configuration.
    /// </summary>
    public OpenAiTextOptions? Text { get; init; }

    /// <summary>
    /// Gets or sets the input conversation items.
    /// </summary>
    public IReadOnlyCollection<OpenAiInputMessage> Input { get; init; } = [];

    /// <summary>
    /// Gets or sets the tool definitions available to the model.
    /// </summary>
    public IReadOnlyCollection<OpenAiToolDefinition>? Tools { get; init; }

    /// <summary>
    /// Gets or sets how the model should choose tools.
    /// </summary>
    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; init; }
}

/// <summary>
/// Represents a single input message in the Responses API.
/// </summary>
public sealed class OpenAiInputMessage
{
    public string Role { get; init; } = string.Empty;

    public IReadOnlyCollection<OpenAiInputContent> Content { get; init; } = [];
}

/// <summary>
/// Represents one content item inside an input message.
/// </summary>
public sealed class OpenAiInputContent
{
    public string Type { get; init; } = "input_text";

    public string Text { get; init; } = string.Empty;
}

/// <summary>
/// Represents text-format configuration for the Responses API.
/// </summary>
public sealed class OpenAiTextOptions
{
    public OpenAiJsonSchemaFormat? Format { get; init; }
}

/// <summary>
/// Represents the json_schema output format configuration.
/// </summary>
public sealed class OpenAiJsonSchemaFormat
{
    public string Type { get; init; } = "json_schema";

    public string Name { get; init; } = string.Empty;

    public bool Strict { get; init; }

    public object Schema { get; init; } = new();
}

/// <summary>
/// Represents one function tool available to the Responses API.
/// </summary>
public sealed class OpenAiToolDefinition
{
    public string Type { get; init; } = "function";

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool Strict { get; init; }

    public object Parameters { get; init; } = new();
}
