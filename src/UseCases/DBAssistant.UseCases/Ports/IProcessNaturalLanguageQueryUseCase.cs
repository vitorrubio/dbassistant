using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.Ports;

/// <summary>
/// Defines the application use case responsible for translating a natural-language question into SQL and optionally executing it.
/// </summary>
public interface IProcessNaturalLanguageQueryUseCase
{
    /// <summary>
    /// Executes the end-to-end natural-language query flow for the supplied request.
    /// </summary>
    /// <param name="request">The request describing the user question and execution intent.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the operation.</param>
    /// <returns>The generated SQL, execution metadata, and optional result set.</returns>
    Task<QueryAssistantResponse> ExecuteAsync(QueryAssistantRequest request, CancellationToken cancellationToken);
}
