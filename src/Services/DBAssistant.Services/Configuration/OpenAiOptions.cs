namespace DBAssistant.Services.Configuration;

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public string Model { get; set; } = string.Empty;
}
