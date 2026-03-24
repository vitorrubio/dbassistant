using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;

namespace DBAssistant.UseCases.IntegrationTests.Fakes;

/// <summary>
/// Provides a deterministic SQL-generation gateway for integration tests.
/// </summary>
public sealed class FakeSqlGenerationGateway : ISqlGenerationGateway
{
    /// <summary>
    /// Returns a fixed SQL-generation result for the supplied test question.
    /// </summary>
    /// <param name="question">The user question supplied to the fake gateway.</param>
    /// <param name="schemaContext">The schema context assembled for the test.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the fake call.</param>
    /// <returns>A fixed SQL-generation result that is easy to assert in tests.</returns>
    public Task<GeneratedSqlResult> GenerateSqlAsync(string question, string schemaContext, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GeneratedSqlResult
        {
            Sql = "SELECT Id, Total FROM Orders",
            Explanation = $"Generated for question: {question}"
        });
    }

    /// <summary>
    /// Returns a deterministic natural-language summary for the executed result set.
    /// </summary>
    /// <param name="question">The original user question.</param>
    /// <param name="sql">The validated SQL executed by the use case.</param>
    /// <param name="executionResult">The tabular result produced by the fake executor.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the fake call.</param>
    /// <returns>A deterministic summary string suitable for test assertions.</returns>
    public Task<QueryResultNarration> GenerateResultsAsTextAsync(
        string question,
        string sql,
        QueryExecutionResult executionResult,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new QueryResultNarration
        {
            ResultsAsText = $"Summary for '{question}' with {executionResult.Rows.Count} row(s)."
        });
    }
}
