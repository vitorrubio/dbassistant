namespace DBAssistant.Api.Configuration;

public sealed class QueryAssistantResponse
{
    public string Sql { get; init; } = string.Empty;

    public string Explanation { get; init; } = string.Empty;

    public string SchemaContextSource { get; init; } = string.Empty;

    public bool Executed { get; init; }

    public IReadOnlyCollection<string> Columns { get; init; } = [];

    public IReadOnlyCollection<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];
}
