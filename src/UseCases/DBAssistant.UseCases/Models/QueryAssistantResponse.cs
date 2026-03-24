namespace DBAssistant.UseCases.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the HTTP response payload returned by the assistant query endpoint.
/// </summary>
public sealed class QueryAssistantResponse
{
    /// <summary>
    /// Gets or sets the validated SQL generated for the request.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sql { get; init; }

    /// <summary>
    /// Gets or sets the explanation describing how the SQL was generated.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Explanation { get; init; }

    /// <summary>
    /// Gets or sets the identifier that describes where the schema context came from.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SchemaContextSource { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the SQL was executed.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Executed { get; init; }

    /// <summary>
    /// Gets or sets the ordered list of returned column names.
    /// </summary>
    public IReadOnlyCollection<string> Columns { get; init; } = [];

    /// <summary>
    /// Gets or sets the returned rows using column-name dictionaries.
    /// </summary>
    public IReadOnlyCollection<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];

    /// <summary>
    /// Gets or sets the short natural-language or Markdown summary generated for the query result.
    /// </summary>
    public string ResultsAsText { get; init; } = string.Empty;
}
