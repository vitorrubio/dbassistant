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
            options.Host = configuration["MYSQL_HOST"] ?? "localhost";

            if (int.TryParse(configuration["MYSQL_PORT"], out var port))
            {
                options.Port = port;
            }

            options.Database = configuration["MYSQL_DATABASE"] ?? "Northwind";
            options.Username = configuration["MYSQL_USERNAME"] ?? string.Empty;
            options.Password = configuration["MYSQL_PASSWORD"] ?? string.Empty;
            options.SchemaName = options.Database;
        });

        services.AddDbContext<FinTechXDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>()
                .Value;
            var connectionString = databaseOptions.GetConnectionString();

            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString));
        });

        services.AddScoped<ISchemaMetadataRepository, SchemaMetadataRepository>();
        services.AddScoped<ISqlQueryExecutor, MySqlQueryExecutor>();

        return services;
    }
}
