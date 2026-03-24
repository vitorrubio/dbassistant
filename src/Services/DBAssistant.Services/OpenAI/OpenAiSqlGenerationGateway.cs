using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DBAssistant.Services.Configuration;
using DBAssistant.Services.OpenAI.Contracts;
using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DBAssistant.Services.OpenAI;

/// <summary>
/// Calls the OpenAI Responses API to generate SQL plans and human-friendly result summaries.
/// </summary>
public sealed class OpenAiSqlGenerationGateway : ISqlGenerationGateway
{
    private const string SQL_SYSTEM_INSTRUCTIONS = """
        You are a MySQL SQL assistant for a connected business database.
        You must decide whether the question can be answered correctly from the provided schema.
        Rules:
        - Use MySQL syntax.
        - Never generate INSERT, UPDATE, DELETE, DROP, ALTER, TRUNCATE, MERGE, EXEC, CALL, CREATE, GRANT, or REVOKE.
        - Only use tables and columns present in the provided schema.
        - Never invent columns, business facts, or semantics that are not explicitly supported by the schema.
        - Never reinterpret free-text fields such as notes or descriptions as dates or structured business facts unless the schema explicitly states that meaning.
        - If the schema is insufficient, mark the question as unanswerable and explain why.
        - When the schema is sufficient, call the function with canAnswer=true and provide one read-only SQL statement plus a brief explanation.
        - When the schema is insufficient, call the function with canAnswer=false, sql='', and unavailableDataReason filled.
        """;

    private const string RESULTS_AS_TEXT_SYSTEM_INSTRUCTIONS = """
        You are a business data analyst assistant.
        Convert SQL query results into a short Markdown answer for the original user question.
        Rules:
        - Base the answer only on the provided SQL result data.
        - Prefer concise natural language when the answer is primarily explanatory.
        - Use a Markdown table only when tabular formatting is clearly more useful than prose.
        - Mention concrete values from the result when they support the conclusion.
        - If the result set is empty, say that no records were found.
        - Return strict JSON with property: resultsAsText.
        - Do not wrap the JSON in markdown.
        """;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _cacheOptions;
    private readonly OpenAiOptions _openAiOptions;
    private readonly OpenAiTransportClient _openAiTransportClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiSqlGenerationGateway"/> class.
    /// </summary>
    public OpenAiSqlGenerationGateway(
        IMemoryCache memoryCache,
        IOptions<CacheOptions> cacheOptions,
        IOptions<OpenAiOptions> openAiOptions,
        OpenAiTransportClient openAiTransportClient)
    {
        _memoryCache = memoryCache;
        _cacheOptions = cacheOptions.Value;
        _openAiOptions = openAiOptions.Value;
        _openAiTransportClient = openAiTransportClient;
    }

    /// <inheritdoc />
    public async Task<GeneratedSqlResult> GenerateSqlAsync(
        string question,
        string schemaContext,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"sql-plan::{Normalize(question)}::{Normalize(schemaContext)}";

        if (_memoryCache.TryGetValue(cacheKey, out GeneratedSqlResult? cachedResult))
        {
            return cachedResult ?? new GeneratedSqlResult();
        }

        var response = await _openAiTransportClient.CreateResponseAsync(
            new OpenAiResponsesRequest
            {
                Model = _openAiOptions.Model,
                Instructions = SQL_SYSTEM_INSTRUCTIONS,
                Tools =
                [
                    new OpenAiToolDefinition
                    {
                        Name = "submit_query_plan",
                        Description = "Submit the final SQL execution plan or explain why the question cannot be answered from the schema.",
                        Strict = true,
                        Parameters = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[] { "canAnswer", "sql", "explanation", "unavailableDataReason" },
                            properties = new
                            {
                                canAnswer = new
                                {
                                    type = "boolean"
                                },
                                sql = new
                                {
                                    type = "string"
                                },
                                explanation = new
                                {
                                    type = "string"
                                },
                                unavailableDataReason = new
                                {
                                    type = "string"
                                }
                            }
                        }
                    }
                ],
                ToolChoice = new
                {
                    type = "function",
                    name = "submit_query_plan"
                },
                Input =
                [
                    new OpenAiInputMessage
                    {
                        Role = "user",
                        Content =
                        [
                            new OpenAiInputContent
                            {
                                Text = BuildUserPrompt(question, schemaContext)
                            }
                        ]
                    }
                ]
            },
            cancellationToken);

        var toolCall = response.Output.FirstOrDefault(item =>
            string.Equals(item.Type, "function_call", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.Name, "submit_query_plan", StringComparison.Ordinal));

        if (string.IsNullOrWhiteSpace(toolCall?.Arguments))
        {
            throw new ExternalServiceUnavailableException("OpenAI did not return the expected SQL planning function call.");
        }

        var result = JsonSerializer.Deserialize<GeneratedSqlResult>(toolCall.Arguments, JsonSerializerOptions);

        if (result is null)
        {
            throw new ExternalServiceUnavailableException("OpenAI returned an invalid SQL planning payload.");
        }

        _memoryCache.Set(
            cacheKey,
            result,
            TimeSpan.FromMinutes(Math.Max(1, _cacheOptions.SqlPlanMinutes)));

        return result;
    }

    /// <inheritdoc />
    public async Task<QueryResultNarration> GenerateResultsAsTextAsync(
        string question,
        string sql,
        QueryExecutionResult executionResult,
        CancellationToken cancellationToken)
    {
        var response = await _openAiTransportClient.CreateResponseAsync(
            new OpenAiResponsesRequest
            {
                Model = _openAiOptions.Model,
                Instructions = RESULTS_AS_TEXT_SYSTEM_INSTRUCTIONS,
                Text = new OpenAiTextOptions
                {
                    Format = new OpenAiJsonSchemaFormat
                    {
                        Name = "query_result_narration",
                        Strict = true,
                        Schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[] { "resultsAsText" },
                            properties = new
                            {
                                resultsAsText = new
                                {
                                    type = "string"
                                }
                            }
                        }
                    }
                },
                Input =
                [
                    new OpenAiInputMessage
                    {
                        Role = "user",
                        Content =
                        [
                            new OpenAiInputContent
                            {
                                Text = BuildResultsAsTextPrompt(question, sql, executionResult)
                            }
                        ]
                    }
                ]
            },
            cancellationToken);

        var outputText = response.OutputText
            ?? response.Output
                .SelectMany(item => item.Content)
                .FirstOrDefault(item => string.Equals(item.Type, "output_text", StringComparison.OrdinalIgnoreCase))
                ?.Text;

        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new ExternalServiceUnavailableException("OpenAI returned an empty response when summarizing the SQL result.");
        }

        var narration = JsonSerializer.Deserialize<QueryResultNarration>(outputText, JsonSerializerOptions);

        if (narration is null)
        {
            throw new ExternalServiceUnavailableException("OpenAI returned an invalid results-as-text payload.");
        }

        return narration;
    }

    private static string BuildUserPrompt(string question, string schemaContext)
    {
        return $"""
            User question:
            {question}

            Available schema:
            {schemaContext}
            """;
    }

    private static string BuildResultsAsTextPrompt(string question, string sql, QueryExecutionResult executionResult)
    {
        var serializedResult = JsonSerializer.Serialize(
            new
            {
                columns = executionResult.Columns,
                rows = executionResult.Rows
            },
            JsonSerializerOptions);

        var builder = new StringBuilder();
        builder.AppendLine("Original user question:");
        builder.AppendLine(question);
        builder.AppendLine();
        builder.AppendLine("Executed SQL:");
        builder.AppendLine(sql);
        builder.AppendLine();
        builder.AppendLine("SQL result payload:");
        builder.AppendLine(serializedResult);
        builder.AppendLine();
        builder.Append("Generate the best short Markdown answer for the user. Use prose when that is clearer than a table.");
        return builder.ToString();
    }

    private static string Normalize(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes);
    }
}
