using DBAssistant.UseCases.Ports;
using DBAssistant.UseCases.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace DBAssistant.UseCases.DependencyInjection;

/// <summary>
/// Registers use-case services in the application dependency-injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the use-case layer services required by the application flow.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<ISchemaContextAssembler, SchemaContextAssembler>();
        services.AddScoped<IProcessNaturalLanguageQueryUseCase, ProcessNaturalLanguageQueryUseCase>();
        return services;
    }
}
