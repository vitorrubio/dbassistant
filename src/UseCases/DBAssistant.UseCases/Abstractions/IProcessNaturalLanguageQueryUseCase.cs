using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.Abstractions;

public interface IProcessNaturalLanguageQueryUseCase
{
    Task<NaturalLanguageQueryResponse> ExecuteAsync(NaturalLanguageQueryRequest request, CancellationToken cancellationToken);
}
