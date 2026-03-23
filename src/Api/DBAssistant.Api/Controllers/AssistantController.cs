using DBAssistant.Domain.Exceptions;
using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;
using Microsoft.AspNetCore.Mvc;

namespace DBAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>
/// Exposes the HTTP endpoint that translates natural-language questions into read-only SQL and optional result sets.
/// </summary>
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

    [HttpPost("query")]
    [ProducesResponseType(typeof(QueryAssistantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    /// <summary>
    /// Processes a natural-language question, generates read-only SQL, and optionally executes it.
    /// </summary>
    /// <param name="request">The request payload containing the user question and execution intent.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the request.</param>
    /// <returns>An HTTP response with the generated SQL and optional execution results.</returns>
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
        catch (ExternalServiceUnavailableException exception)
        {
            return Problem(detail: exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
