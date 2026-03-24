namespace DBAssistant.UseCases.Models;

/// <summary>
/// Represents the short human-friendly summary generated from the SQL result set.
/// </summary>
public sealed class QueryResultNarration
{
    /// <summary>
    /// Gets or sets the short Markdown text or table generated for the result set.
    /// </summary>
    public string ResultsAsText { get; init; } = string.Empty;
}
