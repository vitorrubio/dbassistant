using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;

namespace DBAssistant.UseCases.IntegrationTests.Fakes;

/// <summary>
/// Simulates a SQL generation result that correctly identifies the question as unanswerable from the schema.
/// </summary>
public sealed class FakeSqlGenerationGatewayWithUnavailableData : ISqlGenerationGateway
{
    /// <inheritdoc />
    public Task<GeneratedSqlResult> GenerateSqlAsync(string question, string schemaContext, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GeneratedSqlResult
        {
            CanAnswer = false,
            Sql = string.Empty,
            Explanation = "The requested analysis depends on fields that do not exist in the schema.",
            UnavailableDataReason = "The employees table does not contain a hire date or tenure field, so the question cannot be answered correctly."
        });
    }

    /// <inheritdoc />
    public Task<QueryResultNarration> GenerateResultsAsTextAsync(string question, string sql, QueryExecutionResult executionResult, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Results narration should not be requested for an unanswerable question.");
    }
}
