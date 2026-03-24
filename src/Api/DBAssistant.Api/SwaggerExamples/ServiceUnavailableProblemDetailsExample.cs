using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;

namespace DBAssistant.Api.SwaggerExamples;

/// <summary>
/// Provides a representative external dependency failure payload for Swagger.
/// </summary>
public sealed class ServiceUnavailableProblemDetailsExample : IExamplesProvider<ProblemDetails>
{
    /// <inheritdoc />
    public ProblemDetails GetExamples()
    {
        return new ProblemDetails
        {
            Title = "Service Unavailable",
            Status = StatusCodes.Status503ServiceUnavailable,
            Detail = "OpenAI request failed with status code 503."
        };
    }
}
