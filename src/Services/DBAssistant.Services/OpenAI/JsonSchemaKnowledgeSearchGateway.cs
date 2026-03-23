using System.Text.Json;
using DBAssistant.Services.Configuration;
using DBAssistant.UseCases.Abstractions;
using DBAssistant.UseCases.Models;
using Microsoft.Extensions.Options;

namespace DBAssistant.Services.SchemaKnowledge;

public sealed class JsonSchemaKnowledgeSearchGateway : ISchemaKnowledgeSearchGateway
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SchemaKnowledgeOptions _schemaKnowledgeOptions;

    public JsonSchemaKnowledgeSearchGateway(IOptions<SchemaKnowledgeOptions> schemaKnowledgeOptions)
    {
        _schemaKnowledgeOptions = schemaKnowledgeOptions.Value;
    }

    public async Task<IReadOnlyCollection<SchemaKnowledgeDocument>> SearchAsync(string question, CancellationToken cancellationToken)
    {
        if (File.Exists(_schemaKnowledgeOptions.FilePath) is false)
        {
            return [];
        }

        await using var stream = File.OpenRead(_schemaKnowledgeOptions.FilePath);
        var documents = await JsonSerializer.DeserializeAsync<List<SchemaKnowledgeDocument>>(
            stream,
            JsonSerializerOptions,
            cancellationToken);

        if (documents is null || documents.Count == 0)
        {
            return [];
        }

        var keywords = question
            .Split([' ', '\n', '\r', '\t', ',', '.', ';', ':', '?', '!'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(keyword => keyword.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return documents
            .Select(document => new
            {
                Document = document,
                Score = CalculateScore(document, keywords)
            })
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .Take(Math.Max(1, _schemaKnowledgeOptions.MaxDocuments))
            .Select(result => result.Document)
            .ToArray();
    }

    private static int CalculateScore(SchemaKnowledgeDocument document, IReadOnlyCollection<string> keywords)
    {
        var score = 0;

        foreach (var keyword in keywords)
        {
            if (document.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                score += 3;
            }

            if (document.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                score += 2;
            }

            if (document.TableNames.Any(tableName => tableName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                score += 4;
            }
        }

        return score;
    }
}
