using DBAssistant.Services.Configuration;
using DBAssistant.Services.OpenAI;
using DBAssistant.UseCases.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DBAssistant.Services.DependencyInjection;

/// <summary>
/// Registers service-layer dependencies such as the OpenAI gateway.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds external service integrations to the service collection.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="configuration">The application configuration used to bind service settings.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.Configure<OpenAiOptions>(options =>
        {
            options.ApiKey = configuration["OPENAI_API_KEY"] ?? string.Empty;
            options.BaseUrl = configuration["OPENAI_BASE_URL"] ?? "https://api.openai.com/v1";
            options.Model = configuration["OPENAI_MODEL"] ?? "gpt-5.4";
        });

        services.Configure<CacheOptions>(options =>
        {
            if (int.TryParse(configuration["CACHE_SQL_PLAN_MINUTES"], out var sqlPlanMinutes))
            {
                options.SqlPlanMinutes = sqlPlanMinutes;
            }
        });

        services.AddHttpClient<OpenAiTransportClient>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(90);
        });
        services.AddScoped<ISqlGenerationGateway, OpenAiSqlGenerationGateway>();

        return services;
    }
}
