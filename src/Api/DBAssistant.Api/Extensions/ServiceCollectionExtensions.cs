using Microsoft.OpenApi.Models;

namespace DBAssistant.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiConfiguration(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DB Assistant API",
                Version = "v1",
                Description = "API for translating natural language into safe read-only MySQL queries."
            });
        });

        return services;
    }
}
