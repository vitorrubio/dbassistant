using DBAssistant.Domain.Repositories;

namespace DBAssistant.UseCases.IntegrationTests.Fakes;

/// <summary>
/// Provides a deterministic information-schema reader for use case integration tests.
/// </summary>
public sealed class FakeInformationSchemaReader : IInformationSchemaReader
{
    /// <summary>
    /// Returns a fixed schema snapshot used by the integration tests.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used to stop the fake operation.</param>
    /// <returns>A fixed prompt-friendly schema text.</returns>
    public Task<string> ReadSchemaAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(
            """
            Table: Orders
              - Id (int)
              - CustomerId (varchar)
              - Total (decimal)
            """);
    }
}
