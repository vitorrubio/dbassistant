namespace DBAssistant.Domain.Exceptions;

/// <summary>
/// Represents a domain-level validation failure detected while protecting the application invariants.
/// </summary>
public sealed class DomainValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class.
    /// </summary>
    /// <param name="message">The validation message that explains the violated domain rule.</param>
    public DomainValidationException(string message)
        : base(message)
    {
    }
}
