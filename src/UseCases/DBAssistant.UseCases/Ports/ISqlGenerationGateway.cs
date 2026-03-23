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
}
