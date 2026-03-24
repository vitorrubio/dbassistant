using DBAssistant.Data.Configuration;
using DBAssistant.Domain.Entities;
using DBAssistant.UseCases.Exceptions;
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
        var warnings = new List<string>();

        await using var connection = new MySqlConnection(_databaseOptions.GetServerConnectionString());
        await connection.OpenAsync(cancellationToken);
        await MySqlSchemaResolver.ResolveAndChangeDatabaseAsync(connection, _databaseOptions, cancellationToken);

        try
        {
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
        }
        catch (MySqlException exception)
        {
            throw new QueryExecutionException($"The generated SQL could not be executed by MySQL: {exception.Message}");
        }

        warnings.AddRange(await ReadWarningsAsync(connection, cancellationToken));

        return new QueryExecutionResult
        {
            Columns = columns,
            Rows = rows,
            Warnings = warnings
        };
    }

    private static async Task<IReadOnlyCollection<string>> ReadWarningsAsync(
        MySqlConnection connection,
        CancellationToken cancellationToken)
    {
        var warnings = new List<string>();

        await using var warningCountCommand = new MySqlCommand("SHOW COUNT(*) WARNINGS;", connection);
        var warningCountResult = await warningCountCommand.ExecuteScalarAsync(cancellationToken);

        if (warningCountResult is null ||
            int.TryParse(warningCountResult.ToString(), out var warningCount) is false ||
            warningCount <= 0)
        {
            return warnings;
        }

        await using var warningsCommand = new MySqlCommand("SHOW WARNINGS;", connection);
        await using var reader = await warningsCommand.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.FieldCount < 3)
            {
                continue;
            }

            var level = await reader.IsDBNullAsync(0, cancellationToken) ? string.Empty : reader.GetValue(0)?.ToString();
            var code = await reader.IsDBNullAsync(1, cancellationToken) ? string.Empty : reader.GetValue(1)?.ToString();
            var message = await reader.IsDBNullAsync(2, cancellationToken) ? string.Empty : reader.GetValue(2)?.ToString();

            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            warnings.Add($"{level} {code}: {message}".Trim());
        }

        return warnings;
    }
}
