using DBAssistant.Data.Configuration;
using DBAssistant.Domain.Entities;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace DBAssistant.Data.Services;

/// <summary>
/// Executes validated read-only SQL statements directly against a MySQL database connection.
/// </summary>
public sealed class MySqlQueryExecutor : ISqlQueryExecutor
{
    private readonly DatabaseOptions _databaseOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlQueryExecutor"/> class.
    /// </summary>
    /// <param name="databaseOptions">The database connection settings.</param>
    public MySqlQueryExecutor(IOptions<DatabaseOptions> databaseOptions)
    {
        _databaseOptions = databaseOptions.Value;
    }

    /// <summary>
    /// Executes a validated read-only SQL statement and maps the result to a tabular structure.
    /// </summary>
    /// <param name="sqlStatement">The validated SQL to execute.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the command execution.</param>
    /// <returns>A tabular result containing ordered columns and row dictionaries.</returns>
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
