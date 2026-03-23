namespace DBAssistant.UseCases.Models;

public sealed class NaturalLanguageQueryRequest
{
    public string Question { get; init; } = string.Empty;

    public bool ExecuteSql { get; init; } = true;
}
