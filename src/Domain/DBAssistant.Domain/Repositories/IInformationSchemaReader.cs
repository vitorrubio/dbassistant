namespace DBAssistant.Domain.Repositories;

public interface IInformationSchemaReader
{
    Task<string> ReadSchemaAsync(CancellationToken cancellationToken);
}
