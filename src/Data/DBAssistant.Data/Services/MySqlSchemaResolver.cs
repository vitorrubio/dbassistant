using DBAssistant.Data.Configuration;
using MySqlConnector;

namespace DBAssistant.Data.Services;

/// <summary>
/// Resolves the actual schema name available on the server using a case-insensitive lookup.
/// </summary>
public static class MySqlSchemaResolver
{
    /// <summary>
    /// Resolves the target schema and switches the open connection to it.
    /// </summary>
    /// <param name="connection">The open MySQL connection.</param>
    /// <param name="databaseOptions">The configured database options.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the operation.</param>
    /// <returns>The resolved schema name.</returns>
    public static async Task<string> ResolveAndChangeDatabaseAsync(
        MySqlConnection connection,
        DatabaseOptions databaseOptions,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT SCHEMA_NAME
            FROM INFORMATION_SCHEMA.SCHEMATA
            WHERE LOWER(SCHEMA_NAME) = LOWER(@schemaName)
            LIMIT 1;
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@schemaName", databaseOptions.Database);

        var resolvedSchema = await command.ExecuteScalarAsync(cancellationToken) as string ?? databaseOptions.Database;
        await connection.ChangeDatabaseAsync(resolvedSchema, cancellationToken);

        return resolvedSchema;
    }
}
