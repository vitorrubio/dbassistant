namespace DBAssistant.UseCases.Exceptions;

public sealed class ExternalServiceUnavailableException : Exception
{
    public ExternalServiceUnavailableException(string message)
        : base(message)
    {
    }
}
