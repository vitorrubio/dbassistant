namespace DBAssistant.UseCases.Models;

/// <summary>
/// Represents the tabular result returned by a read-only SQL execution.
/// </summary>
public sealed class QueryExecutionResult
{
    /// <summary>
    /// Gets or sets the ordered list of columns returned by the database query.
    /// </summary>
    public IReadOnlyCollection<string> Columns { get; init; } = [];

    /// <summary>
    /// Gets or sets the query rows as dictionaries keyed by column name.
    /// </summary>
    public IReadOnlyCollection<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];

    /// <summary>
    /// Gets or sets any warning messages returned by the database while executing the query.
    /// </summary>
    public IReadOnlyCollection<string> Warnings { get; init; } = [];
}
