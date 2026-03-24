using DBAssistant.Domain.Entities;
using System.Text;
using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;

namespace DBAssistant.UseCases.UseCases;

/// <summary>
/// Implements the application flow that assembles schema context, generates SQL, validates it, and optionally executes it.
/// </summary>
public sealed class ProcessNaturalLanguageQueryUseCase : IProcessNaturalLanguageQueryUseCase
{
    private const int MAX_RESULTS_AS_TEXT_ROWS = 5;
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

        if (request.ExecuteSql is false)
        {
            return new QueryAssistantResponse
            {
                Sql = sqlStatement.Value,
                Explanation = generatedSql.Explanation,
                SchemaContextSource = schemaContextEnvelope.Source,
                Executed = false
            };
        }

        var executionResult = await _sqlQueryExecutor.ExecuteReadOnlyAsync(sqlStatement, cancellationToken);

        return new QueryAssistantResponse
        {
            Sql = sqlStatement.Value,
            Explanation = generatedSql.Explanation,
            SchemaContextSource = schemaContextEnvelope.Source,
            Executed = true,
            Columns = executionResult.Columns,
            Rows = executionResult.Rows,
            ResultsAsText = BuildResultsAsText(executionResult)
        };
    }

    /// <summary>
    /// Formats the first rows of the SQL result as a Markdown table for quick human inspection.
    /// </summary>
    /// <param name="executionResult">The tabular SQL execution result.</param>
    /// <returns>A Markdown table containing up to five rows, followed by an ellipsis when more rows exist.</returns>
    private static string BuildResultsAsText(QueryExecutionResult executionResult)
    {
        if (executionResult.Columns.Count == 0 || executionResult.Rows.Count == 0)
        {
            return string.Empty;
        }

        var columns = executionResult.Columns.ToArray();
        var rows = executionResult.Rows.Take(MAX_RESULTS_AS_TEXT_ROWS).ToArray();
        var builder = new StringBuilder();

        builder.Append("| ");
        builder.Append(string.Join(" | ", columns.Select(EscapeMarkdownCell)));
        builder.AppendLine(" |");

        builder.Append("| ");
        builder.Append(string.Join(" | ", columns.Select(_ => "---")));
        builder.AppendLine(" |");

        foreach (var row in rows)
        {
            builder.Append("| ");
            builder.Append(string.Join(" | ", columns.Select(column => FormatCellValue(row, column))));
            builder.AppendLine(" |");
        }

        if (executionResult.Rows.Count > MAX_RESULTS_AS_TEXT_ROWS)
        {
            builder.Append("...");
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// Formats one cell value from a result row as safe Markdown text.
    /// </summary>
    /// <param name="row">The row dictionary containing the SQL result values.</param>
    /// <param name="column">The column name to render.</param>
    /// <returns>The formatted cell text.</returns>
    private static string FormatCellValue(IReadOnlyDictionary<string, object?> row, string column)
    {
        if (row.TryGetValue(column, out var value) is false || value is null)
        {
            return "(null)";
        }

        return EscapeMarkdownCell(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
    }

    /// <summary>
    /// Escapes Markdown table control characters and collapses line breaks for safe inline rendering.
    /// </summary>
    /// <param name="value">The raw text value to render inside a Markdown table cell.</param>
    /// <returns>The escaped Markdown-safe text.</returns>
    private static string EscapeMarkdownCell(string value)
    {
        return value
            .Replace("\r\n", "<br/>", StringComparison.Ordinal)
            .Replace("\n", "<br/>", StringComparison.Ordinal)
            .Replace("\r", "<br/>", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal);
    }
}
