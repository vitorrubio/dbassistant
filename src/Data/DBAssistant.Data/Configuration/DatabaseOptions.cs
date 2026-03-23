namespace DBAssistant.Data.Configuration;

public sealed class DatabaseOptions
{
    public const string SECTION_NAME = "Database";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 3306;

    public string Database { get; set; } = "Northwind";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string SchemaName { get; set; } = "Northwind";

    public string GetConnectionString()
    {
        return $"Server={Host};Port={Port};Database={Database};User ID={Username};Password={Password};";
    }
}
