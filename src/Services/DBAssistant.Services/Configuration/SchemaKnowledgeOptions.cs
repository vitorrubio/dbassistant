namespace DBAssistant.Services.Configuration;

public sealed class SchemaKnowledgeOptions
{
    public string FilePath { get; set; } = "knowledge/schema-index.json";

    public int MaxDocuments { get; set; } = 3;
}
