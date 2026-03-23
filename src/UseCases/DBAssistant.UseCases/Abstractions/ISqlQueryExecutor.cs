using DBAssistant.Domain.Entities;
using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.Abstractions;

public interface ISqlQueryExecutor
{
    Task<QueryExecutionResult> ExecuteReadOnlyAsync(SqlStatement sqlStatement, CancellationToken cancellationToken);
}
