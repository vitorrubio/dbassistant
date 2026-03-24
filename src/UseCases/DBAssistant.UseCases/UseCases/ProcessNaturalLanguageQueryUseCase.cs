using DBAssistant.Domain.Entities;
using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;

namespace DBAssistant.UseCases.UseCases;

/// <summary>
/// Implements the application flow that assembles schema context, generates SQL, validates it, and optionally executes it.
/// </summary>
public sealed class ProcessNaturalLanguageQueryUseCase : IProcessNaturalLanguageQueryUseCase
{
    private readonly ISchemaContextAssembler _schemaContextAssembler;
    private readonly ISqlGenerationGateway _sqlGenerationGateway;
    private readonly ISqlQueryExecutor _sqlQueryExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessNaturalLanguageQueryUseCase"/> class.
    /// </summary>
    /// <param name="schemaContextAssembler">The service that prepares schema context for the model.</param>
    /// <param name="sqlGenerationGateway">The gateway that generates SQL from a natural-language question.</param>
    /// <param name="sqlQueryExecutor">The executor that runs validated read-only SQL against the database.</param>
    public ProcessNaturalLanguageQueryUseCase(
        ISchemaContextAssembler schemaContextAssembler,
        ISqlGenerationGateway sqlGenerationGateway,
        ISqlQueryExecutor sqlQueryExecutor)
    {
        _schemaContextAssembler = schemaContextAssembler;
        _sqlGenerationGateway = sqlGenerationGateway;
        _sqlQueryExecutor = sqlQueryExecutor;
    }

    /// <summary>
    /// Executes the natural-language query workflow from question validation to optional database execution.
    /// </summary>
    /// <param name="request">The incoming request containing the user question and execution preference.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the workflow.</param>
    /// <returns>The generated SQL, schema context origin, and optional execution output.</returns>
    /// <exception cref="ApplicationValidationException">Thrown when the question is missing.</exception>
    public async Task<QueryAssistantResponse> ExecuteAsync(
        QueryAssistantRequest request,
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
        var shouldExecuteSql = request.ExecuteSql ?? true;
        var shouldShowDetails = request.ShowDetails ?? false;

        if (shouldExecuteSql is false)
        {
            return new QueryAssistantResponse
            {
                Sql = shouldShowDetails ? sqlStatement.Value : null,
                Explanation = shouldShowDetails ? generatedSql.Explanation : null,
                SchemaContextSource = schemaContextEnvelope.Source,
                Executed = false
            };
        }

        var executionResult = await _sqlQueryExecutor.ExecuteReadOnlyAsync(sqlStatement, cancellationToken);
        var narration = await _sqlGenerationGateway.GenerateResultsAsTextAsync(
            request.Question.Trim(),
            sqlStatement.Value,
            executionResult,
            cancellationToken);

        return new QueryAssistantResponse
        {
            Sql = shouldShowDetails ? sqlStatement.Value : null,
            Explanation = shouldShowDetails ? generatedSql.Explanation : null,
            SchemaContextSource = schemaContextEnvelope.Source,
            Executed = true,
            Columns = executionResult.Columns,
            Rows = executionResult.Rows,
            ResultsAsText = narration.ResultsAsText
        };
    }
}
