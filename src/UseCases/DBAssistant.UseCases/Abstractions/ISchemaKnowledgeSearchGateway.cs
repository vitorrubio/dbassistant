using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.Abstractions;

public interface ISchemaKnowledgeSearchGateway
{
    Task<IReadOnlyCollection<SchemaKnowledgeDocument>> SearchAsync(string question, CancellationToken cancellationToken);
}
