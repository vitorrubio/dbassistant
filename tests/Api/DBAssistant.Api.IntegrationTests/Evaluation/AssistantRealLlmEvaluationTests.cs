using DBAssistant.Api.Controllers;
using DBAssistant.Data.DependencyInjection;
using DBAssistant.Services.DependencyInjection;
using DBAssistant.UseCases.DependencyInjection;
using DBAssistant.UseCases.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DBAssistant.Api.IntegrationTests.Evaluation;

/// <summary>
/// Exercises the real LLM generation path for the official assessment prompts and compares the API result to direct database execution.
/// </summary>
public sealed class AssistantRealLlmEvaluationTests
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

    private const string RunRealLlmEvaluationTestsKey = "RUN_REAL_LLM_EVALUATION_TESTS";

    private static readonly Acceptance.AcceptanceScenario[] ScenarioItems =
    [
        new()
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
        new()
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
        new()
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
        new()
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
        new()
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
        new()
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
        new()
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
        new()
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
        new()
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
        new()
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
        }
    ];

    public static TheoryData<Acceptance.AcceptanceScenario> Scenarios => new(ScenarioItems);

    [Theory]
    [MemberData(nameof(Scenarios))]
    [Trait("Category", "EvaluationTests")]
    public async Task QueryAsync_ShouldMatchDirectDatabaseResult_WhenUsingRealLlm(Acceptance.AcceptanceScenario scenario)
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
        services.AddScoped<AssistantController>();

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var controller = scope.ServiceProvider.GetRequiredService<AssistantController>();
        var directExecutor = new Acceptance.AcceptanceDatabaseExecutor(BuildConnectionString(envValues));

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

    private static IReadOnlyDictionary<string, string> ResolveEnvironmentValues(string repositoryRoot)
    {
        var values = new Dictionary<string, string>(
            Acceptance.AcceptanceEnvFileReader.ReadOptional(Path.Combine(repositoryRoot, ".env")),
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

    private static bool HasRequiredConfiguration(IReadOnlyDictionary<string, string> envValues)
    {
        if (envValues.TryGetValue(RunRealLlmEvaluationTestsKey, out var runRealLlmEvaluationTests) is false ||
            string.Equals(runRealLlmEvaluationTests, "true", StringComparison.OrdinalIgnoreCase) is false)
        {
            return false;
        }

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
