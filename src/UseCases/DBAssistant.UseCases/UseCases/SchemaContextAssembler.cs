using DBAssistant.Domain.Repositories;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;

namespace DBAssistant.UseCases.UseCases;

/// <summary>
/// Formats live information-schema metadata into a prompt-friendly context payload.
/// </summary>
public sealed class SchemaContextAssembler : ISchemaContextAssembler
{
    private const string INFORMATION_SCHEMA_ONLY = "information_schema";
    private const string CUSTOMERS_COMPANY_MARKER = "Table: customers";
    private const string SUPPLIERS_COMPANY_MARKER = "Table: suppliers";
    private const string COMPANY_COLUMN_MARKER = "  - company (";
    private readonly IInformationSchemaReader _informationSchemaReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaContextAssembler"/> class.
    /// </summary>
    /// <param name="informationSchemaReader">The reader that returns live metadata from the connected database.</param>
    public SchemaContextAssembler(IInformationSchemaReader informationSchemaReader)
    {
        _informationSchemaReader = informationSchemaReader;
    }

    /// <summary>
    /// Builds schema context for a user question using live metadata from INFORMATION_SCHEMA.
    /// </summary>
    /// <param name="question">The natural-language question used to retrieve schema context.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the operation.</param>
    /// <returns>A schema context envelope that identifies live metadata as the source.</returns>
    public async Task<SchemaContextEnvelope> BuildAsync(string question, CancellationToken cancellationToken)
    {
        var readableSchema = await _informationSchemaReader.ReadSchemaAsync(cancellationToken);
        var enrichedContext = BuildEnrichedContext(readableSchema);

        return new SchemaContextEnvelope
        {
            Context = enrichedContext,
            Source = INFORMATION_SCHEMA_ONLY
        };
    }

    private static string BuildEnrichedContext(string readableSchema)
    {
        if (string.IsNullOrWhiteSpace(readableSchema))
        {
            return readableSchema;
        }

        if (ContainsCompanyBackedBusinessEntity(readableSchema) is false)
        {
            return readableSchema;
        }

        return $$"""
            {{readableSchema}}

            Interpretation hints:
              - In customer-like or supplier-like tables, a non-empty company column represents an organization or business-account name.
              - If a question refers to corporate or business customers and there is no separate corporate flag, do not reject the question for that reason alone.
              - Prefer company as the customer or supplier display name when it is populated.
            """;
    }

    private static bool ContainsCompanyBackedBusinessEntity(string readableSchema)
    {
        return (readableSchema.Contains(CUSTOMERS_COMPANY_MARKER, StringComparison.OrdinalIgnoreCase) ||
                readableSchema.Contains(SUPPLIERS_COMPANY_MARKER, StringComparison.OrdinalIgnoreCase)) &&
               readableSchema.Contains(COMPANY_COLUMN_MARKER, StringComparison.OrdinalIgnoreCase);
    }
}
