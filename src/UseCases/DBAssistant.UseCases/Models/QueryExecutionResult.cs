namespace DBAssistant.UseCases.Models;

public sealed class QueryExecutionResult
{
    public IReadOnlyCollection<string> Columns { get; init; } = [];

    public IReadOnlyCollection<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];
}
