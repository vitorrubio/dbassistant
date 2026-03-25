using DBAssistant.Api.Controllers;
using DBAssistant.Data.DependencyInjection;
using DBAssistant.UseCases.DependencyInjection;
using DBAssistant.Services.DependencyInjection;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DBAssistant.Api.IntegrationTests.Acceptance;

/// <summary>
/// Executes the acceptance scenarios defined for the assistant by calling <see cref="AssistantController.QueryAsync"/> against the real database.
/// </summary>
public sealed class AssistantAcceptanceTests
{
    private static readonly string[] RequiredEnvironmentKeys =
    [
        "MYSQL_HOST",
        "MYSQL_PORT",
        "MYSQL_DATABASE",
        "MYSQL_USERNAME",
        "MYSQL_PASSWORD",
        "OPENAI_API_KEY",
        "OPENAI_BASE_URL",
        "OPENAI_MODEL"
    ];

    private static readonly AcceptanceScenario[] ScenarioItems =
    [
        new AcceptanceScenario
        {
            Question = "Quais são os produtos mais populares entre os clientes corporativos?",
            Sql = """
                SELECT
                    p.id AS product_id,
                    p.product_name,
                    CAST(SUM(od.quantity) AS DECIMAL(18,2)) AS total_quantity_sold
                FROM order_details od
                INNER JOIN orders o ON o.id = od.order_id
                INNER JOIN customers c ON c.id = o.customer_id
                INNER JOIN products p ON p.id = od.product_id
                WHERE c.company IS NOT NULL
                  AND TRIM(c.company) <> ''
                GROUP BY p.id, p.product_name
                ORDER BY total_quantity_sold DESC, p.product_name ASC;
                """
        },
        new AcceptanceScenario
        {
            Question = "Quais são os produtos mais vendidos em termos de quantidade?",
            Sql = """
                SELECT
                    p.id AS product_id,
                    p.product_name,
                    CAST(SUM(od.quantity) AS DECIMAL(18,2)) AS total_quantity_sold
                FROM order_details od
                INNER JOIN products p ON p.id = od.product_id
                GROUP BY p.id, p.product_name
                ORDER BY total_quantity_sold DESC, p.product_name ASC;
                """
        },
        new AcceptanceScenario
        {
            Question = "Qual é o volume de vendas por cidade?",
            Sql = """
                SELECT
                    COALESCE(c.city, 'Unknown') AS city,
                    CAST(SUM(od.quantity * od.unit_price * (1 - od.discount)) AS DECIMAL(18,2)) AS total_sales
                FROM order_details od
                INNER JOIN orders o ON o.id = od.order_id
                INNER JOIN customers c ON c.id = o.customer_id
                GROUP BY COALESCE(c.city, 'Unknown')
                ORDER BY total_sales DESC, city ASC;
                """
        },
        new AcceptanceScenario
        {
            Question = "Quais são os clientes que mais compraram?",
            Sql = """
                SELECT
                    c.id AS customer_id,
                    COALESCE(NULLIF(TRIM(c.company), ''), CONCAT_WS(' ', c.first_name, c.last_name)) AS customer_name,
                    CAST(SUM(od.quantity * od.unit_price * (1 - od.discount)) AS DECIMAL(18,2)) AS total_sales
                FROM order_details od
                INNER JOIN orders o ON o.id = od.order_id
                INNER JOIN customers c ON c.id = o.customer_id
                GROUP BY c.id, customer_name
                ORDER BY total_sales DESC, customer_name ASC;
                """
        },
        new AcceptanceScenario
        {
            Question = "Quais são os produtos mais caros da loja?",
            Sql = """
                SELECT
                    p.id AS product_id,
                    p.product_name,
                    p.list_price
                FROM products p
                ORDER BY p.list_price DESC, p.product_name ASC;
                """
        },
        new AcceptanceScenario
        {
            Question = "Quais são os fornecedores mais frequentes nos pedidos?",
            Sql = """
                SELECT
                    s.id AS supplier_id,
                    COALESCE(NULLIF(TRIM(s.company), ''), CONCAT_WS(' ', s.first_name, s.last_name)) AS supplier_name,
                    COUNT(po.id) AS purchase_order_count
                FROM purchase_orders po
                INNER JOIN suppliers s ON s.id = po.supplier_id
                GROUP BY s.id, supplier_name
                ORDER BY purchase_order_count DESC, supplier_name ASC;
                """
        },
        new AcceptanceScenario
        {
            Question = "Quais os melhores vendedores?",
            Sql = """
                SELECT
                    e.id AS employee_id,
                    CONCAT_WS(' ', e.first_name, e.last_name) AS employee_name,
                    CAST(SUM(od.quantity * od.unit_price * (1 - od.discount)) AS DECIMAL(18,2)) AS total_sales
                FROM order_details od
                INNER JOIN orders o ON o.id = od.order_id
                INNER JOIN employees e ON e.id = o.employee_id
                GROUP BY e.id, employee_name
                ORDER BY total_sales DESC, employee_name ASC;
                """
        },
        new AcceptanceScenario
        {
            Question = "Qual é o valor total de todas as vendas realizadas por ano?",
            Sql = """
                SELECT
                    YEAR(o.order_date) AS sales_year,
                    CAST(SUM(od.quantity * od.unit_price * (1 - od.discount)) AS DECIMAL(18,2)) AS total_sales
                FROM order_details od
                INNER JOIN orders o ON o.id = od.order_id
                WHERE o.order_date IS NOT NULL
                GROUP BY YEAR(o.order_date)
                ORDER BY sales_year ASC;
                """
        },
        new AcceptanceScenario
        {
            Question = "Qual é o valor total de vendas por categoria de produto?",
            Sql = """
                SELECT
                    COALESCE(NULLIF(TRIM(p.category), ''), 'Uncategorized') AS category,
                    CAST(SUM(od.quantity * od.unit_price * (1 - od.discount)) AS DECIMAL(18,2)) AS total_sales
                FROM order_details od
                INNER JOIN products p ON p.id = od.product_id
                GROUP BY COALESCE(NULLIF(TRIM(p.category), ''), 'Uncategorized')
                ORDER BY total_sales DESC, category ASC;
                """
        },
        new AcceptanceScenario
        {
            Question = "Qual o ticket médio por compra?",
            Sql = """
                SELECT
                    CAST(AVG(order_totals.order_total) AS DECIMAL(18,2)) AS average_ticket
                FROM (
                    SELECT
                        o.id AS order_id,
                        SUM(od.quantity * od.unit_price * (1 - od.discount)) AS order_total
                    FROM orders o
                    INNER JOIN order_details od ON od.order_id = o.id
                    GROUP BY o.id
                ) AS order_totals;
                """
        },
        new AcceptanceScenario
        {
            Question = "Qual é o pior vendedor em quantidade de vendas?",
            Sql = """
                SELECT
                    e.id AS employee_id,
                    CONCAT_WS(' ', e.first_name, e.last_name) AS employee_name,
                    CAST(SUM(od.quantity) AS DECIMAL(18,2)) AS total_quantity_sold
                FROM order_details od
                INNER JOIN orders o ON o.id = od.order_id
                INNER JOIN employees e ON e.id = o.employee_id
                GROUP BY e.id, employee_name
                ORDER BY total_quantity_sold ASC, employee_name ASC;
                """
        },
        new AcceptanceScenario
        {
            Question = "Qual é o pior vendedor em valor total de vendas no último mês?",
            Sql = """
                SELECT
                    employee_sales.employee_id,
                    employee_sales.employee_name,
                    employee_sales.total_sales
                FROM (
                    SELECT
                        e.id AS employee_id,
                        CONCAT_WS(' ', e.first_name, e.last_name) AS employee_name,
                        CAST(SUM(od.quantity * od.unit_price * (1 - od.discount)) AS DECIMAL(18,2)) AS total_sales
                    FROM order_details od
                    INNER JOIN orders o ON o.id = od.order_id
                    INNER JOIN employees e ON e.id = o.employee_id
                    CROSS JOIN (
                        SELECT MAX(order_date) AS max_order_date
                        FROM orders
                        WHERE order_date IS NOT NULL
                    ) latest_order
                    WHERE o.order_date IS NOT NULL
                      AND o.order_date >= DATE_SUB(latest_order.max_order_date, INTERVAL 1 MONTH)
                    GROUP BY e.id, employee_name
                ) AS employee_sales
                ORDER BY employee_sales.total_sales ASC, employee_sales.employee_name ASC;
                """
        }
    ];

    /// <summary>
    /// Gets the acceptance scenarios that define the natural-language questions and their deterministic SQL counterparts.
    /// </summary>
    public static TheoryData<AcceptanceScenario> Scenarios => new(ScenarioItems);

    /// <summary>
    /// Executes each acceptance scenario through the controller and compares the API result to a direct database query.
    /// </summary>
    /// <param name="scenario">The acceptance scenario being executed.</param>
    [Theory]
    [MemberData(nameof(Scenarios))]
    [Trait("Category", "AcceptanceTests")]
    public async Task QueryAsync_ShouldReturnSameResultsAsDirectDatabaseExecution(AcceptanceScenario scenario)
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var envValues = ResolveEnvironmentValues(repositoryRoot);

        if (HasRequiredConfiguration(envValues) is false)
        {
            return;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(envValues.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddUseCases();
        services.AddData(configuration);
        services.AddExternalServices(configuration);

        var scenarios = ScenarioItems.ToDictionary(item => item.Question, StringComparer.Ordinal);
        services.AddSingleton<IReadOnlyDictionary<string, AcceptanceScenario>>(scenarios);
        services.AddScoped<ISqlGenerationGateway, AcceptanceSqlGenerationGateway>();
        services.AddScoped<AssistantController>();

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var controller = scope.ServiceProvider.GetRequiredService<AssistantController>();
        var directExecutor = new AcceptanceDatabaseExecutor(BuildConnectionString(envValues));

        var actionResult = await controller.QueryAsync(
            new QueryAssistantRequest
            {
                Question = scenario.Question,
                ExecuteSql = true,
                ShowDetails = true
            },
            CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<QueryAssistantResponse>().Subject;
        var expected = await directExecutor.ExecuteAsync(scenario.Sql, CancellationToken.None);

        response.Sql.Should().Be(scenario.Sql.Trim());
        response.Executed.Should().BeTrue();
        response.ResultsAsText.Should().NotBeNullOrWhiteSpace();
        response.Columns.Should().BeEquivalentTo(expected.Columns, options => options.WithStrictOrdering());
        response.Rows.Should().BeEquivalentTo(expected.Rows, options => options.WithStrictOrdering());
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DBAssistant.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to resolve the repository root from the test execution directory.");
    }

    private static string BuildConnectionString(IReadOnlyDictionary<string, string> envValues)
    {
        return $"Server={envValues["MYSQL_HOST"]};Port={envValues["MYSQL_PORT"]};User ID={envValues["MYSQL_USERNAME"]};Password={envValues["MYSQL_PASSWORD"]};";
    }

    /// <summary>
    /// Resolves the acceptance-test environment values from the repository dotenv file and process environment variables.
    /// </summary>
    /// <param name="repositoryRoot">The resolved repository root path.</param>
    /// <returns>The merged environment values used by the acceptance test setup.</returns>
    private static IReadOnlyDictionary<string, string> ResolveEnvironmentValues(string repositoryRoot)
    {
        var values = new Dictionary<string, string>(
            AcceptanceEnvFileReader.ReadOptional(Path.Combine(repositoryRoot, ".env")),
            StringComparer.OrdinalIgnoreCase);

        foreach (var key in RequiredEnvironmentKeys)
        {
            var environmentValue = Environment.GetEnvironmentVariable(key);

            if (string.IsNullOrWhiteSpace(environmentValue) is false)
            {
                values[key] = environmentValue;
            }
        }

        return values;
    }

    /// <summary>
    /// Determines whether the acceptance tests have the required runtime configuration to query the real database and OpenAI.
    /// </summary>
    /// <param name="envValues">The merged environment values collected for the tests.</param>
    /// <returns><see langword="true"/> when all required keys are present with non-placeholder values; otherwise, <see langword="false"/>.</returns>
    private static bool HasRequiredConfiguration(IReadOnlyDictionary<string, string> envValues)
    {
        foreach (var key in RequiredEnvironmentKeys)
        {
            if (envValues.TryGetValue(key, out var value) is false ||
                string.IsNullOrWhiteSpace(value) ||
                value.Contains("placeholder", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("your_", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
