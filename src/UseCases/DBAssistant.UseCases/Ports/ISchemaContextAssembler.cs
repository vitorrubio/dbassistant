using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.Ports;

/// <summary>
/// Defines the orchestration contract for building the schema context delivered to the language model.
/// </summary>
public interface ISchemaContextAssembler
{
    /// <summary>
    /// Builds a schema context tailored to the supplied question by combining indexed knowledge and live metadata.
    /// </summary>
    /// <param name="question">The user question that drives context retrieval.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the operation.</param>
    /// <returns>A schema context envelope that contains the assembled text and the source metadata.</returns>
    Task<SchemaContextEnvelope> BuildAsync(string question, CancellationToken cancellationToken);
}
