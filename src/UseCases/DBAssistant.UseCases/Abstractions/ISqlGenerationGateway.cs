using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.Abstractions;

public interface ISqlGenerationGateway
{
    Task<GeneratedSqlResult> GenerateSqlAsync(string question, string schemaContext, CancellationToken cancellationToken);
}
