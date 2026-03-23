namespace DBAssistant.Data.Configuration;

public sealed class DatabaseOptions
{
    public const string SECTION_NAME = "Database";

    public string ConnectionString { get; init; } = string.Empty;

    public string SchemaName { get; init; } = "Northwind";
}
