namespace DBAssistant.UseCases.Models;

/// <summary>
/// Represents the structured output returned by the SQL generation gateway.
/// </summary>
public sealed class GeneratedSqlResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the question can be answered with the available schema.
    /// </summary>
    public bool CanAnswer { get; init; } = true;

    /// <summary>
    /// Gets or sets the SQL text generated for the user's question.
    /// </summary>
    public string Sql { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the natural-language explanation that describes the generated SQL strategy.
    /// </summary>
    public string Explanation { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason explaining why the question cannot be answered from the available schema.
    /// </summary>
    public string UnavailableDataReason { get; init; } = string.Empty;
}
