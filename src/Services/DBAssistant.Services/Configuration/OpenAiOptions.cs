namespace DBAssistant.Services.Configuration;

public sealed class OpenAiOptions
{
    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "https://api.openai.com/v1";

    public string Model { get; init; } = string.Empty;
}
