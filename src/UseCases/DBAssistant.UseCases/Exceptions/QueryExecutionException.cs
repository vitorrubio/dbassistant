namespace DBAssistant.UseCases.Exceptions;

/// <summary>
/// Represents a failure caused by an invalid generated SQL statement during database execution.
/// </summary>
public sealed class QueryExecutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryExecutionException"/> class.
    /// </summary>
    /// <param name="message">The message describing why execution failed.</param>
    public QueryExecutionException(string message)
        : base(message)
    {
    }
}
