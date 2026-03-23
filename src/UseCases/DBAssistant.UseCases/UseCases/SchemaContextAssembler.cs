using System.Text;
using DBAssistant.Domain.Repositories;
using DBAssistant.UseCases.Abstractions;
using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.UseCases;

public sealed class SchemaContextAssembler : ISchemaContextAssembler
{
    private const string RAG_AND_INFORMATION_SCHEMA = "rag+information_schema";
    private const string INFORMATION_SCHEMA_ONLY = "information_schema";
    private readonly IInformationSchemaReader _informationSchemaReader;
    private readonly ISchemaKnowledgeSearchGateway _schemaKnowledgeSearchGateway;

    public SchemaContextAssembler(
        IInformationSchemaReader informationSchemaReader,
        ISchemaKnowledgeSearchGateway schemaKnowledgeSearchGateway)
    {
        _informationSchemaReader = informationSchemaReader;
        _schemaKnowledgeSearchGateway = schemaKnowledgeSearchGateway;
    }

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
