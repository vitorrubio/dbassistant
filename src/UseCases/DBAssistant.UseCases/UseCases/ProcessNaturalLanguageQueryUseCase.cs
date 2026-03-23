using DBAssistant.Domain.Entities;
using DBAssistant.Domain.Repositories;
using DBAssistant.UseCases.Abstractions;
using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.UseCases;

public sealed class ProcessNaturalLanguageQueryUseCase : IProcessNaturalLanguageQueryUseCase
{
    private readonly ISchemaMetadataRepository _schemaMetadataRepository;
    private readonly ISqlGenerationGateway _sqlGenerationGateway;
    private readonly ISqlQueryExecutor _sqlQueryExecutor;

    public ProcessNaturalLanguageQueryUseCase(
        ISchemaMetadataRepository schemaMetadataRepository,
        ISqlGenerationGateway sqlGenerationGateway,
        ISqlQueryExecutor sqlQueryExecutor)
    {
        _schemaMetadataRepository = schemaMetadataRepository;
        _sqlGenerationGateway = sqlGenerationGateway;
        _sqlQueryExecutor = sqlQueryExecutor;
    }

    public async Task<NaturalLanguageQueryResponse> ExecuteAsync(
        NaturalLanguageQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            throw new ApplicationValidationException("The question is required.");
        }

        var schemaContext = await _schemaMetadataRepository.GetReadableSchemaAsync(cancellationToken);
        var generatedSql = await _sqlGenerationGateway.GenerateSqlAsync(request.Question.Trim(), schemaContext, cancellationToken);
        var sqlStatement = SqlStatement.CreateReadOnly(generatedSql.Sql);

        if (request.ExecuteSql is false)
        {
            return new NaturalLanguageQueryResponse
            {
                Sql = sqlStatement.Value,
                Explanation = generatedSql.Explanation,
                Executed = false
            };
        }

        var executionResult = await _sqlQueryExecutor.ExecuteReadOnlyAsync(sqlStatement, cancellationToken);

        return new NaturalLanguageQueryResponse
        {
            Sql = sqlStatement.Value,
            Explanation = generatedSql.Explanation,
            Executed = true,
            Columns = executionResult.Columns,
            Rows = executionResult.Rows
        };
    }
}
