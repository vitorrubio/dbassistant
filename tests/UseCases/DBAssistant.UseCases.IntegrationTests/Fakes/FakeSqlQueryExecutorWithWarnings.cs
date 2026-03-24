using DBAssistant.Domain.Entities;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;

namespace DBAssistant.UseCases.IntegrationTests.Fakes;

/// <summary>
/// Simulates a database execution that returns warnings instead of a clean result.
/// </summary>
public sealed class FakeSqlQueryExecutorWithWarnings : ISqlQueryExecutor
{
    /// <inheritdoc />
    public Task<QueryExecutionResult> ExecuteReadOnlyAsync(SqlStatement sqlStatement, CancellationToken cancellationToken)
    {
        return Task.FromResult(new QueryExecutionResult
        {
            Warnings =
            [
                "Warning 1411: Incorrect datetime value: 'Joined the company as a sales representative.' for function str_to_date"
            ]
        });
    }
}
