using MySqlConnector;

namespace DBAssistant.Api.IntegrationTests.Acceptance;

/// <summary>
/// Executes SQL directly against the configured MySQL database so acceptance tests can compare controller output to raw database results.
/// </summary>
public sealed class AcceptanceDatabaseExecutor
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptanceDatabaseExecutor"/> class.
    /// </summary>
    /// <param name="connectionString">The MySQL connection string used for direct validation queries.</param>
    public AcceptanceDatabaseExecutor(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Executes the supplied SQL and returns the result in the same tabular shape used by the API.
    /// </summary>
    /// <param name="sql">The SQL statement to execute directly against the database.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the command.</param>
    /// <returns>The ordered columns and rows returned by the database.</returns>
    public async Task<(IReadOnlyCollection<string> Columns, IReadOnlyCollection<IReadOnlyDictionary<string, object?>> Rows)> ExecuteAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        var columns = new List<string>();
        var rows = new List<IReadOnlyDictionary<string, object?>>();

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await ResolveAndChangeDatabaseAsync(connection, cancellationToken);

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        for (var columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
        {
            columns.Add(reader.GetName(columnIndex));
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (var columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
            {
                row[reader.GetName(columnIndex)] = await reader.IsDBNullAsync(columnIndex, cancellationToken)
                    ? null
                    : reader.GetValue(columnIndex);
            }

            rows.Add(row);
        }

        return (columns, rows);
    }

    private static async Task ResolveAndChangeDatabaseAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT SCHEMA_NAME
            FROM INFORMATION_SCHEMA.SCHEMATA
            WHERE LOWER(SCHEMA_NAME) = LOWER(@schemaName)
            LIMIT 1;
            """;

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@schemaName", "Northwind");

        var resolvedSchema = await command.ExecuteScalarAsync(cancellationToken) as string ?? "northwind";
        await connection.ChangeDatabaseAsync(resolvedSchema, cancellationToken);
    }
}
