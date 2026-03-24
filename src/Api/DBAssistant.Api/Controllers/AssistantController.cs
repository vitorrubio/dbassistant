using DBAssistant.Domain.Exceptions;
using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using DBAssistant.Api.SwaggerExamples;

namespace DBAssistant.Api.Controllers;

/// <summary>
/// Exposes the HTTP endpoint that translates natural-language questions into read-only SQL and optional result sets.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AssistantController : ControllerBase
{
    private readonly IProcessNaturalLanguageQueryUseCase _processNaturalLanguageQueryUseCase;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantController"/> class.
    /// </summary>
    /// <param name="processNaturalLanguageQueryUseCase">The application use case that handles the assistant workflow.</param>
    public AssistantController(IProcessNaturalLanguageQueryUseCase processNaturalLanguageQueryUseCase)
    {
        _processNaturalLanguageQueryUseCase = processNaturalLanguageQueryUseCase;
    }

    /// <summary>
    /// Processes a natural-language question, generates read-only SQL, and optionally executes it.
    /// </summary>
    /// <param name="request">The request payload containing the user question and execution intent.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the request.</param>
    /// <returns>An HTTP response with the generated SQL and optional execution results.</returns>
    [HttpPost("query", Name = "QueryAssistant")]
    [SwaggerOperation(
        OperationId = "QueryAssistant",
        Summary = "Translate natural language into safe read-only SQL",
        Description = "Builds schema context, asks the LLM for a safe SQL plan, optionally executes the query, and returns both raw rows and a short business-friendly summary.")]
    [SwaggerRequestExample(typeof(QueryAssistantRequest), typeof(AssistantQueryRequestExample))]
    [ProducesResponseType(typeof(QueryAssistantResponse), StatusCodes.Status200OK)]
    [SwaggerResponse(StatusCodes.Status200OK, "The assistant generated and optionally executed a read-only SQL query.", typeof(QueryAssistantResponse))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(AssistantQueryResponseExample))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "The question cannot be answered safely from the schema or the generated SQL was invalid.", typeof(ProblemDetails))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationProblemDetailsExample))]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "The LLM provider or another external dependency is unavailable.", typeof(ProblemDetails))]
    [SwaggerResponseExample(StatusCodes.Status503ServiceUnavailable, typeof(ServiceUnavailableProblemDetailsExample))]
    public async Task<IActionResult> QueryAsync(
        [FromBody] QueryAssistantRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _processNaturalLanguageQueryUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ApplicationValidationException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
        catch (DomainValidationException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
        catch (QueryExecutionException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
        catch (ExternalServiceUnavailableException exception)
        {
            return Problem(detail: exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
