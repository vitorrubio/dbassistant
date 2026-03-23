using DBAssistant.UseCases.Abstractions;
using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.IntegrationTests.Fakes;

public sealed class FakeSchemaKnowledgeSearchGateway : ISchemaKnowledgeSearchGateway
{
    private readonly IReadOnlyCollection<SchemaKnowledgeDocument> _documents;

    public FakeSchemaKnowledgeSearchGateway(params SchemaKnowledgeDocument[] documents)
    {
        _documents = documents;
    }

    public Task<IReadOnlyCollection<SchemaKnowledgeDocument>> SearchAsync(string question, CancellationToken cancellationToken)
    {
        return Task.FromResult(_documents);
    }
}
