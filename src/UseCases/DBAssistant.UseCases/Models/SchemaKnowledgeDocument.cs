namespace DBAssistant.UseCases.Models;

public sealed class SchemaKnowledgeDocument
{
    public string Title { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public IReadOnlyCollection<string> TableNames { get; init; } = [];
}
