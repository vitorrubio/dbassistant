using DBAssistant.Domain.Repositories;

namespace DBAssistant.UseCases.IntegrationTests.Fakes;

public sealed class FakeInformationSchemaReader : IInformationSchemaReader
{
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
