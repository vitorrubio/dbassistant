using System.Text;
using DBAssistant.Domain.Repositories;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;

namespace DBAssistant.UseCases.UseCases;

/// <summary>
/// Combines indexed schema knowledge and live information-schema metadata into a prompt-friendly context payload.
/// </summary>
public sealed class SchemaContextAssembler : ISchemaContextAssembler
{
    private const string RAG_AND_INFORMATION_SCHEMA = "rag+information_schema";
    private const string INFORMATION_SCHEMA_ONLY = "information_schema";
    private readonly IInformationSchemaReader _informationSchemaReader;
    private readonly ISchemaKnowledgeSearchGateway _schemaKnowledgeSearchGateway;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaContextAssembler"/> class.
    /// </summary>
    /// <param name="informationSchemaReader">The reader that returns live metadata from the connected database.</param>
    /// <param name="schemaKnowledgeSearchGateway">The gateway that retrieves indexed schema knowledge.</param>
    public SchemaContextAssembler(
        IInformationSchemaReader informationSchemaReader,
        ISchemaKnowledgeSearchGateway schemaKnowledgeSearchGateway)
    {
        _informationSchemaReader = informationSchemaReader;
        _schemaKnowledgeSearchGateway = schemaKnowledgeSearchGateway;
    }

    /// <summary>
    /// Builds schema context for a user question by prioritizing indexed knowledge and appending live metadata as fallback.
    /// </summary>
    /// <param name="question">The natural-language question used to retrieve schema context.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the operation.</param>
    /// <returns>A schema context envelope that identifies whether RAG and/or live metadata were used.</returns>
    public async Task<SchemaContextEnvelope> BuildAsync(string question, CancellationToken cancellationToken)
    {
        var schemaKnowledgeDocuments = await _schemaKnowledgeSearchGateway.SearchAsync(question, cancellationToken);
        var readableSchema = await _informationSchemaReader.ReadSchemaAsync(cancellationToken);

        if (schemaKnowledgeDocuments.Count == 0)
        {
            return new SchemaContextEnvelope
            {
                Context = readableSchema,
                Source = INFORMATION_SCHEMA_ONLY
            };
        }

        var builder = new StringBuilder();
        builder.AppendLine("RAG schema knowledge:");

        foreach (var document in schemaKnowledgeDocuments)
        {
            builder.AppendLine($"Document: {document.Title}");
            builder.AppendLine(document.Content);
            builder.AppendLine();
        }

        builder.AppendLine("Authoritative fallback schema from INFORMATION_SCHEMA:");
        builder.AppendLine(readableSchema);

        return new SchemaContextEnvelope
        {
            Context = builder.ToString().TrimEnd(),
            Source = RAG_AND_INFORMATION_SCHEMA
        };
    }
}
