using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;

namespace DBAssistant.Api.SwaggerExamples;

/// <summary>
/// Provides a representative validation error payload for Swagger.
/// </summary>
public sealed class ValidationProblemDetailsExample : IExamplesProvider<ProblemDetails>
{
    /// <inheritdoc />
    public ProblemDetails GetExamples()
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "The employees table does not contain a hire date or tenure field, so the question cannot be answered correctly."
        };
    }
}
