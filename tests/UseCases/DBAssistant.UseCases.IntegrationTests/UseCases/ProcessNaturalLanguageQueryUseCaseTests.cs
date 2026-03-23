using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.IntegrationTests.Fakes;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.UseCases;
using FluentAssertions;
using Xunit;

namespace DBAssistant.UseCases.IntegrationTests.UseCases;

public sealed class ProcessNaturalLanguageQueryUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnRowsWhenExecutionIsEnabled()
    {
        var schemaContextAssembler = new SchemaContextAssembler(
            new FakeInformationSchemaReader(),
            new FakeSchemaKnowledgeSearchGateway(
                new SchemaKnowledgeDocument
                {
                    Title = "Orders document",
                    Content = "Orders can be joined with customers.",
                    TableNames = ["Orders", "Customers"]
                }));

        var useCase = new ProcessNaturalLanguageQueryUseCase(
            schemaContextAssembler,
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
        result.SchemaContextSource.Should().Be("rag+information_schema");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectEmptyQuestion()
    {
        var schemaContextAssembler = new SchemaContextAssembler(
            new FakeInformationSchemaReader(),
            new FakeSchemaKnowledgeSearchGateway());

        var useCase = new ProcessNaturalLanguageQueryUseCase(
            schemaContextAssembler,
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

    [Fact]
    public async Task ExecuteAsync_ShouldFallbackToInformationSchemaWhenRagHasNoMatch()
    {
        var schemaContextAssembler = new SchemaContextAssembler(
            new FakeInformationSchemaReader(),
            new FakeSchemaKnowledgeSearchGateway());

        var useCase = new ProcessNaturalLanguageQueryUseCase(
            schemaContextAssembler,
            new FakeSqlGenerationGateway(),
            new FakeSqlQueryExecutor());

        var result = await useCase.ExecuteAsync(
            new NaturalLanguageQueryRequest
            {
                Question = "List orders",
                ExecuteSql = false
            },
            CancellationToken.None);

        result.Executed.Should().BeFalse();
        result.SchemaContextSource.Should().Be("information_schema");
    }
}
