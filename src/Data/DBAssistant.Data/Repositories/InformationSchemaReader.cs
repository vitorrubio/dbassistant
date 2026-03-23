using System.Text;
using DBAssistant.Data.Configuration;
using DBAssistant.Domain.Repositories;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace DBAssistant.Data.Repositories;

/// <summary>
/// Reads the authoritative schema metadata directly from MySQL <c>INFORMATION_SCHEMA</c> tables.
/// </summary>
public sealed class InformationSchemaReader : IInformationSchemaReader
{
    private readonly DatabaseOptions _databaseOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="InformationSchemaReader"/> class.
    /// </summary>
    /// <param name="databaseOptions">The database connection settings.</param>
    public InformationSchemaReader(IOptions<DatabaseOptions> databaseOptions)
    {
        _databaseOptions = databaseOptions.Value;
    }

    /// <summary>
    /// Reads the current schema definition and formats it for prompt consumption.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to stop the metadata query.</param>
    /// <returns>A text representation of tables, columns, and data types available in the target schema.</returns>
    public async Task<string> ReadSchemaAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @schemaName
            ORDER BY TABLE_NAME, ORDINAL_POSITION;
            """;

        var builder = new StringBuilder();
        var connectionString = _databaseOptions.GetConnectionString();

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@schemaName", _databaseOptions.SchemaName);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        string? currentTable = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            var tableName = reader.GetString("TABLE_NAME");
            var columnName = reader.GetString("COLUMN_NAME");
            var dataType = reader.GetString("DATA_TYPE");

            if (currentTable != tableName)
            {
                currentTable = tableName;
                builder.AppendLine($"Table: {tableName}");
            }

            builder.AppendLine($"  - {columnName} ({dataType})");
        }

        return builder.Length == 0
            ? "No schema metadata was found."
            : builder.ToString().TrimEnd();
    }
}
