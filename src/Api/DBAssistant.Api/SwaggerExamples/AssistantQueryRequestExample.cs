using DBAssistant.UseCases.Models;
using Swashbuckle.AspNetCore.Filters;

namespace DBAssistant.Api.SwaggerExamples;

/// <summary>
/// Provides a representative assistant query request example for Swagger.
/// </summary>
public sealed class AssistantQueryRequestExample : IExamplesProvider<QueryAssistantRequest>
{
    /// <inheritdoc />
    public QueryAssistantRequest GetExamples()
    {
        return new QueryAssistantRequest
        {
            Question = "Quais são os produtos mais vendidos em termos de quantidade?",
            ExecuteSql = true,
            ShowDetails = true
        };
    }
}
