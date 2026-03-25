using DBAssistant.Domain.Repositories;
using DBAssistant.UseCases.UseCases;
using FluentAssertions;
using Xunit;

namespace DBAssistant.UseCases.IntegrationTests.UseCases;

/// <summary>
/// Verifies the schema-context enrichment that is applied before prompting the SQL model.
/// </summary>
public sealed class SchemaContextAssemblerTests
{
    /// <summary>
    /// Ensures company-based business-account guidance is appended when the schema exposes a customers company field.
    /// </summary>
    [Fact]
    [Trait("Category", "IntegrationTests")]
    public async Task BuildAsync_ShouldAppendBusinessAccountHint_WhenCompanyColumnExists()
    {
        var assembler = new SchemaContextAssembler(new InlineInformationSchemaReader(
            """
            Table: customers
              - id (int)
              - company (varchar)
              - first_name (varchar)
              - last_name (varchar)
            Table: orders
              - id (int)
              - customer_id (int)
            """));

        var result = await assembler.BuildAsync(
            "Quais são os produtos mais populares entre os clientes corporativos?",
            CancellationToken.None);

        result.Source.Should().Be("information_schema");
        result.Context.Should().Contain("Interpretation hints:");
        result.Context.Should().Contain("a non-empty company column represents an organization or business-account name");
        result.Context.Should().Contain("do not reject the question for that reason alone");
    }

    private sealed class InlineInformationSchemaReader : IInformationSchemaReader
    {
        private readonly string _schema;

        public InlineInformationSchemaReader(string schema)
        {
            _schema = schema;
        }

        public Task<string> ReadSchemaAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_schema);
        }
    }
}
