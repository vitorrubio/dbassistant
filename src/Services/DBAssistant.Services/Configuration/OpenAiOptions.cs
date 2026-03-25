namespace DBAssistant.Services.Configuration;

/// <summary>
/// Stores the OpenAI configuration required to generate SQL from natural-language questions.
/// </summary>
public sealed class OpenAiOptions
{
    /// <summary>
    /// Gets or sets the API key used to authenticate against OpenAI.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL of the OpenAI-compatible API endpoint.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// Gets or sets the model identifier used for SQL generation.
    /// </summary>
    public string Model { get; set; } = string.Empty;

}
