namespace DBAssistant.Services.Configuration;

public sealed class SchemaKnowledgeOptions
{
    public string FilePath { get; init; } = "knowledge/schema-index.json";

    public int MaxDocuments { get; init; } = 3;
}
