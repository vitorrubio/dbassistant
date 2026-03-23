using DBAssistant.UseCases.Abstractions;
using DBAssistant.UseCases.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace DBAssistant.UseCases.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IProcessNaturalLanguageQueryUseCase, ProcessNaturalLanguageQueryUseCase>();
        return services;
    }
}
