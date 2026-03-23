using DBAssistant.Domain.Entities;
using DBAssistant.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace DBAssistant.Domain.UnitTests.Guards;

public sealed class SqlStatementTests
{
    [Fact]
    public void CreateReadOnly_ShouldAcceptValidSelectStatement()
    {
        var sqlStatement = SqlStatement.CreateReadOnly("SELECT * FROM Orders");

        sqlStatement.Value.Should().Be("SELECT * FROM Orders");
    }

    [Fact]
    public void CreateReadOnly_ShouldAcceptValidCteStatement()
    {
        var sqlStatement = SqlStatement.CreateReadOnly(
            """
            WITH recent_orders AS (
                SELECT * FROM Orders
            )
            SELECT * FROM recent_orders
            """);

        sqlStatement.Value.Should().StartWith("WITH");
    }

    [Fact]
    public void CreateReadOnly_ShouldRejectMutationStatement()
    {
        var action = () => SqlStatement.CreateReadOnly("SELECT * FROM Orders; DELETE FROM Orders;");

        action.Should().Throw<DomainValidationException>();
    }
}
