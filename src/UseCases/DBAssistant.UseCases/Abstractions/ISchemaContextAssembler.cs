using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.Abstractions;

public interface ISchemaContextAssembler
{
    Task<SchemaContextEnvelope> BuildAsync(string question, CancellationToken cancellationToken);
}
