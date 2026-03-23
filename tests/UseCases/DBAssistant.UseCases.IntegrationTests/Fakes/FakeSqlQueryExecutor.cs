using DBAssistant.Domain.Entities;
using DBAssistant.UseCases.Abstractions;
using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.IntegrationTests.Fakes;

public sealed class FakeSqlQueryExecutor : ISqlQueryExecutor
{
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
