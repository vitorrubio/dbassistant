using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.Ports;

/// <summary>
/// Defines the contract for generating SQL from a natural-language question and schema context.
/// </summary>
public interface ISqlGenerationGateway
{
    /// <summary>
    /// Generates SQL for the provided question using the supplied schema context.
    /// </summary>
    /// <param name="question">The natural-language question asked by the user.</param>
    /// <param name="schemaContext">The schema context assembled for the model prompt.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the generation request.</param>
    /// <returns>The generated SQL statement and its explanation.</returns>
    Task<GeneratedSqlResult> GenerateSqlAsync(string question, string schemaContext, CancellationToken cancellationToken);

    /// <summary>
    /// Generates a short human-friendly Markdown summary for the SQL execution output.
    /// </summary>
    /// <param name="question">The original natural-language question.</param>
    /// <param name="sql">The validated SQL that was executed.</param>
    /// <param name="executionResult">The tabular execution result returned by the database.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the generation request.</param>
    /// <returns>A short text or Markdown table describing the query result.</returns>
    Task<QueryResultNarration> GenerateResultsAsTextAsync(
        string question,
        string sql,
        QueryExecutionResult executionResult,
        CancellationToken cancellationToken);
}
