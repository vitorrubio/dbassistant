namespace DBAssistant.Data.Configuration;

/// <summary>
/// Stores the database connection settings used to reach the target MySQL instance.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// Gets or sets the database server host name or IP address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the TCP port used to connect to the MySQL server.
    /// </summary>
    public int Port { get; set; } = 3306;

    /// <summary>
    /// Gets or sets the target database name used for direct SQL access and metadata reading.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database user name used for authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database password used for authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name used while querying the information schema tables.
    /// </summary>
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Builds a MySQL connection string from the configured connection fragments.
    /// </summary>
    /// <returns>A connection string ready to be consumed by <c>MySqlConnection</c>.</returns>
    public string GetConnectionString()
    {
        return $"Server={Host};Port={Port};Database={Database};User ID={Username};Password={Password};";
    }

    /// <summary>
    /// Builds a server-level MySQL connection string without selecting a default database.
    /// </summary>
    /// <returns>A connection string that connects to the server only.</returns>
    public string GetServerConnectionString()
    {
        return $"Server={Host};Port={Port};User ID={Username};Password={Password};";
    }
}
