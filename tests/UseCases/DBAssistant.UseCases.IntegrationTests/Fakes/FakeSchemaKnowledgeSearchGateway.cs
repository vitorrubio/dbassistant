using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;

namespace DBAssistant.UseCases.IntegrationTests.Fakes;

/// <summary>
/// Provides a deterministic schema-knowledge search gateway for integration tests.
/// </summary>
public sealed class FakeSchemaKnowledgeSearchGateway : ISchemaKnowledgeSearchGateway
{
    private readonly IReadOnlyCollection<SchemaKnowledgeDocument> _documents;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeSchemaKnowledgeSearchGateway"/> class.
    /// </summary>
    /// <param name="documents">The fake documents that should be returned by the gateway.</param>
    public FakeSchemaKnowledgeSearchGateway(params SchemaKnowledgeDocument[] documents)
    {
        _documents = documents;
    }

    /// <summary>
    /// Returns the fake schema knowledge documents configured for the test case.
    /// </summary>
    /// <param name="question">The user question supplied to the fake gateway.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the fake call.</param>
    /// <returns>The configured fake schema knowledge documents.</returns>
    public Task<IReadOnlyCollection<SchemaKnowledgeDocument>> SearchAsync(string question, CancellationToken cancellationToken)
    {
        return Task.FromResult(_documents);
    }
}
