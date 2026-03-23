using System.Text;
using DBAssistant.Data.Configuration;
using DBAssistant.Domain.Repositories;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace DBAssistant.Data.Repositories;

public sealed class SchemaMetadataRepository : ISchemaMetadataRepository
{
    private readonly DatabaseOptions _databaseOptions;

    public SchemaMetadataRepository(IOptions<DatabaseOptions> databaseOptions)
    {
        _databaseOptions = databaseOptions.Value;
    }

    public async Task<string> GetReadableSchemaAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @schemaName
            ORDER BY TABLE_NAME, ORDINAL_POSITION;
            """;

        var builder = new StringBuilder();

        await using var connection = new MySqlConnection(_databaseOptions.ConnectionString);
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
