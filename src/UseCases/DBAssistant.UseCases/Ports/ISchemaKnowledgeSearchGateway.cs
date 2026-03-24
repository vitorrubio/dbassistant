using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.Ports;

/// <summary>
/// Defines the contract for searching indexed schema knowledge before falling back to live database metadata.
/// </summary>
public interface ISchemaKnowledgeSearchGateway
{
    /// <summary>
    /// Searches the schema knowledge index for documents that are relevant to the supplied question.
    /// </summary>
    /// <param name="question">The user question used as the retrieval query.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the search.</param>
    /// <returns>A collection of schema knowledge documents ranked as relevant to the query.</returns>
    Task<IReadOnlyCollection<SchemaKnowledgeDocument>> SearchAsync(string question, CancellationToken cancellationToken);
}
