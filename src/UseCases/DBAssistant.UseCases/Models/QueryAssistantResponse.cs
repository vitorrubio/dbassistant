namespace DBAssistant.UseCases.Models;

/// <summary>
/// Represents the HTTP response payload returned by the assistant query endpoint.
/// </summary>
public sealed class QueryAssistantResponse
{
    /// <summary>
    /// Gets or sets the validated SQL generated for the request.
    /// </summary>
    public string Sql { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the explanation describing how the SQL was generated.
    /// </summary>
    public string Explanation { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier that describes where the schema context came from.
    /// </summary>
    public string SchemaContextSource { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the SQL was executed.
    /// </summary>
    public bool Executed { get; init; }

    /// <summary>
    /// Gets or sets the ordered list of returned column names.
    /// </summary>
    public IReadOnlyCollection<string> Columns { get; init; } = [];

    /// <summary>
    /// Gets or sets the returned rows using column-name dictionaries.
    /// </summary>
    public IReadOnlyCollection<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];

    /// <summary>
    /// Gets or sets the top query results rendered as a Markdown table for quick display.
    /// </summary>
    public string ResultsAsText { get; init; } = string.Empty;
}
