namespace DBAssistant.Domain.Repositories;

public interface ISchemaMetadataRepository
{
    Task<string> GetReadableSchemaAsync(CancellationToken cancellationToken);
}
