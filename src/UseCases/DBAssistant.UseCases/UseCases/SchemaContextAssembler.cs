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

        return new SchemaContextEnvelope
        {
            Context = readableSchema,
            Source = INFORMATION_SCHEMA_ONLY
        };
    }
}
