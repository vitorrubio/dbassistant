using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DBAssistant.Services.Configuration;
using DBAssistant.UseCases.Abstractions;
using DBAssistant.UseCases.Exceptions;
using DBAssistant.UseCases.Models;
using Microsoft.Extensions.Options;

namespace DBAssistant.Services.OpenAI;

public sealed class OpenAiSqlGenerationGateway : ISqlGenerationGateway
{
    private const string SYSTEM_INSTRUCTIONS = """
        You are a MySQL SQL assistant for the FinTechX Northwind database.
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

    public OpenAiSqlGenerationGateway(HttpClient httpClient, IOptions<OpenAiOptions> openAiOptions)
    {
        _httpClient = httpClient;
        _openAiOptions = openAiOptions.Value;
    }

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
        var outputText = document.RootElement.TryGetProperty("output_text", out var outputTextElement)
            ? outputTextElement.GetString()
            : null;

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

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_openAiOptions.ApiKey) ||
            string.IsNullOrWhiteSpace(_openAiOptions.BaseUrl) ||
            string.IsNullOrWhiteSpace(_openAiOptions.Model))
        {
            throw new ExternalServiceUnavailableException("OpenAI configuration is incomplete. Fill the .env placeholders before using this endpoint.");
        }
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
}
