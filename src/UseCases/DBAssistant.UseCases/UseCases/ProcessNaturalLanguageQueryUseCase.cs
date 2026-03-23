using DBAssistant.Domain.Entities;
using DBAssistant.UseCases.Abstractions;
using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.Models;

namespace DBAssistant.UseCases.UseCases;

public sealed class ProcessNaturalLanguageQueryUseCase : IProcessNaturalLanguageQueryUseCase
{
    private readonly ISchemaContextAssembler _schemaContextAssembler;
    private readonly ISqlGenerationGateway _sqlGenerationGateway;
    private readonly ISqlQueryExecutor _sqlQueryExecutor;

    public ProcessNaturalLanguageQueryUseCase(
        ISchemaContextAssembler schemaContextAssembler,
        ISqlGenerationGateway sqlGenerationGateway,
        ISqlQueryExecutor sqlQueryExecutor)
    {
        _schemaContextAssembler = schemaContextAssembler;
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

        var schemaContextEnvelope = await _schemaContextAssembler.BuildAsync(request.Question.Trim(), cancellationToken);
        var generatedSql = await _sqlGenerationGateway.GenerateSqlAsync(
            request.Question.Trim(),
            schemaContextEnvelope.Context,
            cancellationToken);
        var sqlStatement = SqlStatement.CreateReadOnly(generatedSql.Sql);

        if (request.ExecuteSql is false)
        {
            return new NaturalLanguageQueryResponse
            {
                Sql = sqlStatement.Value,
                Explanation = generatedSql.Explanation,
                SchemaContextSource = schemaContextEnvelope.Source,
                Executed = false
            };
        }

        var executionResult = await _sqlQueryExecutor.ExecuteReadOnlyAsync(sqlStatement, cancellationToken);

        return new NaturalLanguageQueryResponse
        {
            Sql = sqlStatement.Value,
            Explanation = generatedSql.Explanation,
            SchemaContextSource = schemaContextEnvelope.Source,
            Executed = true,
            Columns = executionResult.Columns,
            Rows = executionResult.Rows
        };
    }
}
