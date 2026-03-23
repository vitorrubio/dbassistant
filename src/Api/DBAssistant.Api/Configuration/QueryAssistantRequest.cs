namespace DBAssistant.Api.Configuration;

public sealed class QueryAssistantRequest
{
    public string Question { get; init; } = string.Empty;

    public bool ExecuteSql { get; init; } = true;
}
