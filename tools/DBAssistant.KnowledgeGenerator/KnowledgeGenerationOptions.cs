namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Stores the options required to connect to the source database and write the knowledge artifact.
/// </summary>
public sealed class KnowledgeGenerationOptions
{
    /// <summary>
    /// Gets or sets the MySQL host name.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MySQL port number.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the target database name.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user name used to connect to the source database.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password used to connect to the source database.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path of the output JSON artifact.
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Builds the runtime MySQL connection string from the configured parts.
    /// </summary>
    /// <returns>A MySQL connection string.</returns>
    public string BuildConnectionString()
    {
        return $"Server={Host};Port={Port};Database={Database};User ID={Username};Password={Password};";
    }

    /// <summary>
    /// Builds a MySQL connection string that connects to the server without selecting a default database.
    /// </summary>
    /// <returns>A server-level MySQL connection string.</returns>
    public string BuildServerConnectionString()
    {
        return $"Server={Host};Port={Port};User ID={Username};Password={Password};";
    }
}
