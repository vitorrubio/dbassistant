using DBAssistant.Domain.Entities;
using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.Ports;

/// <summary>
/// Defines the contract for executing validated read-only SQL statements.
/// </summary>
public interface ISqlQueryExecutor
{
    /// <summary>
    /// Executes a validated read-only SQL statement and returns a tabular result set.
    /// </summary>
    /// <param name="sqlStatement">The validated SQL statement to execute.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the database command.</param>
    /// <returns>The tabular data returned by the connected database.</returns>
    Task<QueryExecutionResult> ExecuteReadOnlyAsync(SqlStatement sqlStatement, CancellationToken cancellationToken);
}
