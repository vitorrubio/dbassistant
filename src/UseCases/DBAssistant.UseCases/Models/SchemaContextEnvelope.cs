namespace DBAssistant.UseCases.Models;

public sealed class SchemaContextEnvelope
{
    public string Context { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;
}
