namespace DBAssistant.UseCases.Models;

/// <summary>
/// Represents the HTTP request payload accepted by the assistant query endpoint.
/// </summary>
public sealed class QueryAssistantRequest
{
    /// <summary>
    /// Gets or sets the natural-language question the caller wants to ask.
    /// </summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the generated SQL should be executed after validation.
    /// </summary>
    public bool? ExecuteSql { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether SQL-generation details should be returned to the caller.
    /// </summary>
    public bool? ShowDetails { get; init; }

}
