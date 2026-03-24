using DBAssistant.UseCases.Models;
using Swashbuckle.AspNetCore.Filters;

namespace DBAssistant.Api.SwaggerExamples;

/// <summary>
/// Provides a representative successful assistant query response example for Swagger.
/// </summary>
public sealed class AssistantQueryResponseExample : IExamplesProvider<QueryAssistantResponse>
{
    /// <inheritdoc />
    public QueryAssistantResponse GetExamples()
    {
        return new QueryAssistantResponse
        {
            Sql = """
                SELECT
                    p.id AS product_id,
                    p.product_name,
                    CAST(SUM(od.quantity) AS DECIMAL(18,2)) AS total_quantity_sold
                FROM order_details od
                INNER JOIN products p ON p.id = od.product_id
                GROUP BY p.id, p.product_name
                ORDER BY total_quantity_sold DESC, p.product_name ASC;
                """.Trim(),
            Explanation = "Aggregates sold quantities per product and orders the result descending by total quantity sold.",
            SchemaContextSource = "rag+information_schema",
            Executed = true,
            Columns = ["product_id", "product_name", "total_quantity_sold"],
            Rows =
            [
                new Dictionary<string, object?>
                {
                    ["product_id"] = 20,
                    ["product_name"] = "Sir Rodney's Marmalade",
                    ["total_quantity_sold"] = 400m
                }
            ],
            ResultsAsText = "O produto mais vendido em quantidade foi **Sir Rodney's Marmalade**, com **400 unidades**."
        };
    }
}
