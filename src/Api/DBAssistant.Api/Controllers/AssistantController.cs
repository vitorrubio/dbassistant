using DBAssistant.Api.Configuration;
using DBAssistant.Domain.Exceptions;
using DBAssistant.UseCases.Abstractions;
using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.Models;
using Microsoft.AspNetCore.Mvc;

namespace DBAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AssistantController : ControllerBase
{
    private readonly IProcessNaturalLanguageQueryUseCase _processNaturalLanguageQueryUseCase;

    public AssistantController(IProcessNaturalLanguageQueryUseCase processNaturalLanguageQueryUseCase)
    {
        _processNaturalLanguageQueryUseCase = processNaturalLanguageQueryUseCase;
    }

    [HttpPost("query")]
    [ProducesResponseType(typeof(QueryAssistantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> QueryAsync(
        [FromBody] QueryAssistantRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _processNaturalLanguageQueryUseCase.ExecuteAsync(
                new NaturalLanguageQueryRequest
                {
                    Question = request.Question,
                    ExecuteSql = request.ExecuteSql
                },
                cancellationToken);

            return Ok(new QueryAssistantResponse
            {
                Sql = response.Sql,
                Explanation = response.Explanation,
                SchemaContextSource = response.SchemaContextSource,
                Executed = response.Executed,
                Columns = response.Columns,
                Rows = response.Rows
            });
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
