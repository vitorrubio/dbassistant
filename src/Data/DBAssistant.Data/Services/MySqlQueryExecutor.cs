using DBAssistant.Data.Configuration;
using DBAssistant.Domain.Entities;
using DBAssistant.UseCases.Abstractions;
using DBAssistant.UseCases.Models;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace DBAssistant.Data.Services;

public sealed class MySqlQueryExecutor : ISqlQueryExecutor
{
    private readonly DatabaseOptions _databaseOptions;

    public MySqlQueryExecutor(IOptions<DatabaseOptions> databaseOptions)
    {
        _databaseOptions = databaseOptions.Value;
    }

    public async Task<QueryExecutionResult> ExecuteReadOnlyAsync(SqlStatement sqlStatement, CancellationToken cancellationToken)
    {
        var columns = new List<string>();
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        var connectionString = _databaseOptions.GetConnectionString();

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(sqlStatement.Value, connection);
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

        return new QueryExecutionResult
        {
            Columns = columns,
            Rows = rows
        };
    }
}
