namespace DBAssistant.UseCases.Exceptions;

/// <summary>
/// Represents an application-level validation failure for an incoming use case request.
/// </summary>
public sealed class ApplicationValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationValidationException"/> class.
    /// </summary>
    /// <param name="message">The validation message that explains why the request is invalid.</param>
    public ApplicationValidationException(string message)
        : base(message)
    {
    }
}
