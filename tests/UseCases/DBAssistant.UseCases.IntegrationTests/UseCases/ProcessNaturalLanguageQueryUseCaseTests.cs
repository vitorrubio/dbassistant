using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.IntegrationTests.Fakes;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.UseCases;
using FluentAssertions;

namespace DBAssistant.UseCases.IntegrationTests.UseCases;

public sealed class ProcessNaturalLanguageQueryUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnRowsWhenExecutionIsEnabled()
    {
        var useCase = new ProcessNaturalLanguageQueryUseCase(
            new FakeSchemaMetadataRepository(),
            new FakeSqlGenerationGateway(),
            new FakeSqlQueryExecutor());

        var result = await useCase.ExecuteAsync(
            new NaturalLanguageQueryRequest
            {
                Question = "Show order totals",
                ExecuteSql = true
            },
            CancellationToken.None);

        result.Executed.Should().BeTrue();
        result.Rows.Should().HaveCount(1);
        result.Sql.Should().Be("SELECT Id, Total FROM Orders");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectEmptyQuestion()
    {
        var useCase = new ProcessNaturalLanguageQueryUseCase(
            new FakeSchemaMetadataRepository(),
            new FakeSqlGenerationGateway(),
            new FakeSqlQueryExecutor());

        var action = async () => await useCase.ExecuteAsync(
            new NaturalLanguageQueryRequest
            {
                Question = string.Empty
            },
            CancellationToken.None);

        await action.Should().ThrowAsync<ApplicationValidationException>();
    }
}
