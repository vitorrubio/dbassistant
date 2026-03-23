using DBAssistant.UseCases.Abstractions;
using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.IntegrationTests.Fakes;

public sealed class FakeSqlGenerationGateway : ISqlGenerationGateway
{
    public Task<GeneratedSqlResult> GenerateSqlAsync(string question, string schemaContext, CancellationToken cancellationToken)
    {
        return Task.FromResult(new GeneratedSqlResult
        {
            Sql = "SELECT Id, Total FROM Orders",
            Explanation = $"Generated for question: {question}"
        });
    }
}
