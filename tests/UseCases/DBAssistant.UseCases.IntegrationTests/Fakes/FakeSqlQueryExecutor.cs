using DBAssistant.Domain.Entities;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;

namespace DBAssistant.UseCases.IntegrationTests.Fakes;

/// <summary>
/// Provides a deterministic SQL executor for use case integration tests.
/// </summary>
public sealed class FakeSqlQueryExecutor : ISqlQueryExecutor
{
    /// <summary>
    /// Returns a fixed tabular result regardless of the SQL supplied by the test.
    /// </summary>
    /// <param name="sqlStatement">The validated SQL statement received by the fake executor.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the fake call.</param>
    /// <returns>A fixed tabular execution result used for assertions.</returns>
    public Task<QueryExecutionResult> ExecuteReadOnlyAsync(SqlStatement sqlStatement, CancellationToken cancellationToken)
    {
        return Task.FromResult(new QueryExecutionResult
        {
            Columns = ["Id", "Total"],
            Rows =
            [
                new Dictionary<string, object?>
                {
                    ["Id"] = 1,
                    ["Total"] = 120.45m
                }
            ]
        });
    }
}
