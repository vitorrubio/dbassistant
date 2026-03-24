using DBAssistant.Data.Configuration;
using DBAssistant.Data.Repositories;
using DBAssistant.Data.Services;
using DBAssistant.Domain.Repositories;
using DBAssistant.UseCases.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DBAssistant.Data.DependencyInjection;

/// <summary>
/// Registers the data-layer services required to read schema metadata and execute SQL commands.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the data-layer dependencies to the service collection.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="configuration">The application configuration used to bind database settings.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(options =>
        {
            options.Host = configuration["MYSQL_HOST"] ?? "localhost";

            if (int.TryParse(configuration["MYSQL_PORT"], out var port))
            {
                options.Port = port;
            }

            options.Database = configuration["MYSQL_DATABASE"] ?? string.Empty;
            options.Username = configuration["MYSQL_USERNAME"] ?? string.Empty;
            options.Password = configuration["MYSQL_PASSWORD"] ?? string.Empty;
            options.SchemaName = options.Database;
        });

        services.AddScoped<IInformationSchemaReader, InformationSchemaReader>();
        services.AddScoped<ISqlQueryExecutor, MySqlQueryExecutor>();

        return services;
    }
}
