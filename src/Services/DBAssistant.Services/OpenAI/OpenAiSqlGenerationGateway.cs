using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DBAssistant.Services.Configuration;
using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;
using Microsoft.Extensions.Options;

namespace DBAssistant.Services.OpenAI;

/// <summary>
/// Calls the OpenAI Responses API to transform a natural-language question into structured SQL output.
/// </summary>
public sealed class OpenAiSqlGenerationGateway : ISqlGenerationGateway
{
    private const string SYSTEM_INSTRUCTIONS = """
        You are a MySQL SQL assistant for a connected business database.
        Convert the user question into a single read-only SQL statement.
        Rules:
        - Use MySQL syntax.
        - Never generate INSERT, UPDATE, DELETE, DROP, ALTER, TRUNCATE, MERGE, EXEC, CALL, CREATE, GRANT, or REVOKE.
        - Only answer using tables and columns present in the provided schema.
        - Prefer explicit joins and aliases when helpful.
        - Return strict JSON with properties: sql, explanation.
        - Do not wrap the JSON in markdown.
        """;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _openAiOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiSqlGenerationGateway"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client configured for the OpenAI endpoint.</param>
    /// <param name="openAiOptions">The OpenAI settings used by the gateway.</param>
    public OpenAiSqlGenerationGateway(HttpClient httpClient, IOptions<OpenAiOptions> openAiOptions)
    {
        _httpClient = httpClient;
        _openAiOptions = openAiOptions.Value;
    }

    /// <summary>
    /// Sends the question and schema context to OpenAI and parses the generated SQL response.
    /// </summary>
    /// <param name="question">The user question expressed in natural language.</param>
    /// <param name="schemaContext">The schema context injected into the prompt.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the HTTP request.</param>
    /// <returns>The SQL and explanation returned by the model.</returns>
    /// <exception cref="ExternalServiceUnavailableException">Thrown when configuration is incomplete or the OpenAI response is invalid.</exception>
    public async Task<GeneratedSqlResult> GenerateSqlAsync(
        string question,
        string schemaContext,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        using var request = new HttpRequestMessage(HttpMethod.Post, "responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = _openAiOptions.Model,
            instructions = SYSTEM_INSTRUCTIONS,
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "generated_sql_result",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "sql", "explanation" },
                        properties = new
                        {
                            sql = new
                            {
                                type = "string",
                                description = "A single read-only MySQL SELECT statement that answers the question."
                            },
                            explanation = new
                            {
                                type = "string",
                                description = "A brief explanation of how the SQL answers the question."
                            }
                        }
                    }
                }
            },
            input = new[]
            {
                new
                {
                    role = "user",
                    content = new[]
                    {
                        new
                        {
                            type = "input_text",
                            text = BuildUserPrompt(question, schemaContext)
                        }
                    }
                }
            }
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode is false)
        {
            throw new ExternalServiceUnavailableException($"OpenAI request failed with status code {(int)response.StatusCode}: {payload}");
        }

        using var document = JsonDocument.Parse(payload);
        var outputText = TryExtractOutputText(document.RootElement);

        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new ExternalServiceUnavailableException("OpenAI returned an empty response.");
        }

        var result = JsonSerializer.Deserialize<GeneratedSqlResult>(outputText, JsonSerializerOptions);

        if (result is null || string.IsNullOrWhiteSpace(result.Sql))
        {
            throw new ExternalServiceUnavailableException("OpenAI returned an invalid SQL payload.");
        }

        return result;
    }

    /// <summary>
    /// Validates that the mandatory OpenAI configuration values were provided.
    /// </summary>
    /// <exception cref="ExternalServiceUnavailableException">Thrown when one or more required settings are missing.</exception>
    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_openAiOptions.ApiKey) ||
            string.IsNullOrWhiteSpace(_openAiOptions.BaseUrl) ||
            string.IsNullOrWhiteSpace(_openAiOptions.Model))
        {
            throw new ExternalServiceUnavailableException("OpenAI configuration is incomplete. Fill the .env placeholders before using this endpoint.");
        }
    }

    /// <summary>
    /// Builds the user-facing prompt block containing the question and assembled schema context.
    /// </summary>
    /// <param name="question">The natural-language question asked by the user.</param>
    /// <param name="schemaContext">The prompt-friendly schema context assembled by the application.</param>
    /// <returns>A formatted prompt string for the OpenAI request body.</returns>
    private static string BuildUserPrompt(string question, string schemaContext)
    {
        return $"""
            User question:
            {question}

            Available schema:
            {schemaContext}
            """;
    }

    /// <summary>
    /// Extracts the textual model output from the Responses API payload, supporting both top-level and nested message formats.
    /// </summary>
    /// <param name="rootElement">The JSON payload returned by the Responses API.</param>
    /// <returns>The textual content generated by the model, or <see langword="null"/> when no text output is available.</returns>
    private static string? TryExtractOutputText(JsonElement rootElement)
    {
        if (rootElement.TryGetProperty("output_text", out var outputTextElement) &&
            outputTextElement.ValueKind == JsonValueKind.String)
        {
            return outputTextElement.GetString();
        }

        if (rootElement.TryGetProperty("output", out var outputElement) is false ||
            outputElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var outputItem in outputElement.EnumerateArray())
        {
            if (outputItem.TryGetProperty("content", out var contentElement) is false ||
                contentElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in contentElement.EnumerateArray())
            {
                if (contentItem.TryGetProperty("type", out var typeElement) is false ||
                    typeElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                if (string.Equals(typeElement.GetString(), "output_text", StringComparison.OrdinalIgnoreCase) is false)
                {
                    continue;
                }

                if (contentItem.TryGetProperty("text", out var textElement) &&
                    textElement.ValueKind == JsonValueKind.String)
                {
                    return textElement.GetString();
                }
            }
        }

        return null;
    }
}
