using DBAssistant.Domain.Entities;
using DBAssistant.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace DBAssistant.Domain.UnitTests.Guards;

/// <summary>
/// Verifies the behavior of the read-only SQL guard.
/// </summary>
public sealed class SqlStatementTests
{
    /// <summary>
    /// Ensures that a simple <c>SELECT</c> statement is accepted by the domain guard.
    /// </summary>
    [Fact]
    public void CreateReadOnly_ShouldAcceptValidSelectStatement()
    {
        var sqlStatement = SqlStatement.CreateReadOnly("SELECT * FROM Orders");

        sqlStatement.Value.Should().Be("SELECT * FROM Orders");
    }

    /// <summary>
    /// Ensures that a read-only statement starting with a CTE is accepted by the domain guard.
    /// </summary>
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

    /// <summary>
    /// Ensures that mutation statements embedded in the SQL text are rejected by the domain guard.
    /// </summary>
    [Fact]
    public void CreateReadOnly_ShouldRejectMutationStatement()
    {
        var action = () => SqlStatement.CreateReadOnly("SELECT * FROM Orders; DELETE FROM Orders;");

        action.Should().Throw<DomainValidationException>();
    }
}
