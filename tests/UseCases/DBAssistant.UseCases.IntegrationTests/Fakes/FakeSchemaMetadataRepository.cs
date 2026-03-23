using DBAssistant.Domain.Repositories;

namespace DBAssistant.UseCases.IntegrationTests.Fakes;

public sealed class FakeSchemaMetadataRepository : ISchemaMetadataRepository
{
    public Task<string> GetReadableSchemaAsync(CancellationToken cancellationToken)
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
