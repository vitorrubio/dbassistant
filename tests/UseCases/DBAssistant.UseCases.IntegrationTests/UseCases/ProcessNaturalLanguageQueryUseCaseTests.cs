using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.IntegrationTests.Fakes;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.UseCases;
using FluentAssertions;
using Xunit;

namespace DBAssistant.UseCases.IntegrationTests.UseCases;

/// <summary>
/// Verifies the orchestration behavior of the natural-language query use case.
/// </summary>
public sealed class ProcessNaturalLanguageQueryUseCaseTests
{
    /// <summary>
    /// Ensures the use case returns rows while keeping metadata hidden by default when execution is enabled.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit Tests")]
    public async Task ExecuteAsync_ShouldExecuteByDefault_WhenExecuteSqlIsOmitted()
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
            new QueryAssistantRequest
            {
                Question = "Show order totals"
            },
            CancellationToken.None);

        result.Executed.Should().BeNull();
        result.Rows.Should().HaveCount(1);
        result.Sql.Should().BeNull();
        result.Explanation.Should().BeNull();
        result.ResultsAsText.Should().Be("Summary for 'Show order totals' with 1 row(s).");
        result.SchemaContextSource.Should().BeNull();
    }

    /// <summary>
    /// Ensures the use case rejects requests without a question.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit Tests")]
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
            new QueryAssistantRequest
            {
                Question = string.Empty
            },
            CancellationToken.None);

        await action.Should().ThrowAsync<ApplicationValidationException>();
    }

    /// <summary>
    /// Ensures the use case hides metadata when execution is disabled and details remain hidden.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit Tests")]
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
            new QueryAssistantRequest
            {
                Question = "List orders",
                ExecuteSql = false
            },
            CancellationToken.None);

        result.Executed.Should().BeNull();
        result.SchemaContextSource.Should().BeNull();
    }

    /// <summary>
    /// Ensures explicit execution disablement preserves the default hidden-details behavior.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit Tests")]
    public async Task ExecuteAsync_ShouldNotExecute_WhenExecuteSqlIsExplicitlyFalse()
    {
        var schemaContextAssembler = new SchemaContextAssembler(
            new FakeInformationSchemaReader(),
            new FakeSchemaKnowledgeSearchGateway());

        var useCase = new ProcessNaturalLanguageQueryUseCase(
            schemaContextAssembler,
            new FakeSqlGenerationGateway(),
            new FakeSqlQueryExecutor());

        var result = await useCase.ExecuteAsync(
            new QueryAssistantRequest
            {
                Question = "List orders",
                ExecuteSql = false
            },
            CancellationToken.None);

        result.Executed.Should().BeNull();
        result.Sql.Should().BeNull();
        result.Explanation.Should().BeNull();
        result.ResultsAsText.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures SQL details are returned when the caller explicitly asks for them.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit Tests")]
    public async Task ExecuteAsync_ShouldReturnSqlAndExplanation_WhenShowDetailsIsTrue()
    {
        var schemaContextAssembler = new SchemaContextAssembler(
            new FakeInformationSchemaReader(),
            new FakeSchemaKnowledgeSearchGateway());

        var useCase = new ProcessNaturalLanguageQueryUseCase(
            schemaContextAssembler,
            new FakeSqlGenerationGateway(),
            new FakeSqlQueryExecutor());

        var result = await useCase.ExecuteAsync(
            new QueryAssistantRequest
            {
                Question = "Show order totals",
                ExecuteSql = true,
                ShowDetails = true
            },
            CancellationToken.None);

        result.Executed.Should().BeTrue();
        result.Sql.Should().Be("SELECT Id, Total FROM Orders");
        result.Explanation.Should().Be("Generated for question: Show order totals");
        result.ResultsAsText.Should().Be("Summary for 'Show order totals' with 1 row(s).");
        result.SchemaContextSource.Should().Be("information_schema");
    }

    /// <summary>
    /// Ensures omitted show-details keeps SQL metadata hidden even when the query executes.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit Tests")]
    public async Task ExecuteAsync_ShouldHideDetailsByDefault_WhenShowDetailsIsOmitted()
    {
        var schemaContextAssembler = new SchemaContextAssembler(
            new FakeInformationSchemaReader(),
            new FakeSchemaKnowledgeSearchGateway());

        var useCase = new ProcessNaturalLanguageQueryUseCase(
            schemaContextAssembler,
            new FakeSqlGenerationGateway(),
            new FakeSqlQueryExecutor());

        var result = await useCase.ExecuteAsync(
            new QueryAssistantRequest
            {
                Question = "Show order totals",
                ExecuteSql = true,
                ShowDetails = null
            },
            CancellationToken.None);

        result.Sql.Should().BeNull();
        result.Explanation.Should().BeNull();
        result.Executed.Should().BeNull();
        result.SchemaContextSource.Should().BeNull();
    }

    /// <summary>
    /// Ensures the use case rejects questions that require data not present in the schema instead of inventing columns.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit Tests")]
    public async Task ExecuteAsync_ShouldRejectQuestion_WhenSchemaCannotAnswerIt()
    {
        var schemaContextAssembler = new SchemaContextAssembler(
            new FakeInformationSchemaReader(),
            new FakeSchemaKnowledgeSearchGateway());

        var useCase = new ProcessNaturalLanguageQueryUseCase(
            schemaContextAssembler,
            new FakeSqlGenerationGatewayWithUnavailableData(),
            new FakeSqlQueryExecutor());

        var action = async () => await useCase.ExecuteAsync(
            new QueryAssistantRequest
            {
                Question = "Which employees have more than one month of tenure?"
            },
            CancellationToken.None);

        await action.Should()
            .ThrowAsync<ApplicationValidationException>()
            .WithMessage("*does not contain a hire date or tenure field*");
    }

    /// <summary>
    /// Ensures database warnings are treated as invalid generated SQL instead of empty data.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit Tests")]
    public async Task ExecuteAsync_ShouldRejectGeneratedSql_WhenDatabaseReturnsWarnings()
    {
        var schemaContextAssembler = new SchemaContextAssembler(
            new FakeInformationSchemaReader(),
            new FakeSchemaKnowledgeSearchGateway());

        var useCase = new ProcessNaturalLanguageQueryUseCase(
            schemaContextAssembler,
            new FakeSqlGenerationGateway(),
            new FakeSqlQueryExecutorWithWarnings());

        var action = async () => await useCase.ExecuteAsync(
            new QueryAssistantRequest
            {
                Question = "Show order totals"
            },
            CancellationToken.None);

        await action.Should()
            .ThrowAsync<ApplicationValidationException>()
            .WithMessage("*produced database warnings*");
    }
}
