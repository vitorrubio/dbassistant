using DBAssistant.Data.Configuration;
using DBAssistant.Data.Persistence;
using DBAssistant.Data.Repositories;
using DBAssistant.Data.Services;
using DBAssistant.Domain.Repositories;
using DBAssistant.UseCases.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DBAssistant.Data.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(options =>
        {
            options.ConnectionString = configuration["MYSQL_CONNECTION_STRING"] ?? string.Empty;
            options.SchemaName = configuration["MYSQL_DATABASE"] ?? "Northwind";
        });

        services.AddDbContext<FinTechXDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>()
                .Value;

            options.UseMySql(
                databaseOptions.ConnectionString,
                ServerVersion.AutoDetect(databaseOptions.ConnectionString));
        });

        services.AddScoped<ISchemaMetadataRepository, SchemaMetadataRepository>();
        services.AddScoped<ISqlQueryExecutor, MySqlQueryExecutor>();

        return services;
    }
}
