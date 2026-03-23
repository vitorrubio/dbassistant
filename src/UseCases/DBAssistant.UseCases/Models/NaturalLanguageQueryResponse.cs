namespace DBAssistant.UseCases.Models;

public sealed class NaturalLanguageQueryResponse
{
    public string Sql { get; init; } = string.Empty;

    public string Explanation { get; init; } = string.Empty;

    public bool Executed { get; init; }

    public IReadOnlyCollection<string> Columns { get; init; } = [];

    public IReadOnlyCollection<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];
}
