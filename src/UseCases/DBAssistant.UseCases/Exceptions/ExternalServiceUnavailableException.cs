namespace DBAssistant.UseCases.Exceptions;

/// <summary>
/// Represents a failure caused by an unavailable or invalid external dependency such as the language model provider.
/// </summary>
public sealed class ExternalServiceUnavailableException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceUnavailableException"/> class.
    /// </summary>
    /// <param name="message">The message describing the external dependency failure.</param>
    public ExternalServiceUnavailableException(string message)
        : base(message)
    {
    }
}
