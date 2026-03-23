namespace DBAssistant.Domain.Repositories;

/// <summary>
/// Defines the contract for reading database structure metadata from the authoritative information schema.
/// </summary>
public interface IInformationSchemaReader
{
    /// <summary>
    /// Reads the current database schema and returns it in a prompt-friendly text format.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to stop the metadata query.</param>
    /// <returns>A readable schema snapshot produced from the connected database.</returns>
    Task<string> ReadSchemaAsync(CancellationToken cancellationToken);
}
