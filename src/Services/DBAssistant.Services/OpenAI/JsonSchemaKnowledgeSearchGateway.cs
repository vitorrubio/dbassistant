using System.Text.Json;
using DBAssistant.Services.Configuration;
using DBAssistant.Services.SchemaKnowledge;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;
using Microsoft.Extensions.Options;

namespace DBAssistant.Services.SchemaKnowledge;

/// <summary>
/// Provides a lightweight JSON-backed schema knowledge search implementation used as the bootstrap RAG source.
/// </summary>
public sealed class JsonSchemaKnowledgeSearchGateway : ISchemaKnowledgeSearchGateway
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SchemaKnowledgeOptions _schemaKnowledgeOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaKnowledgeSearchGateway"/> class.
    /// </summary>
    /// <param name="schemaKnowledgeOptions">The configuration that points to the JSON knowledge index.</param>
    public JsonSchemaKnowledgeSearchGateway(IOptions<SchemaKnowledgeOptions> schemaKnowledgeOptions)
    {
        _schemaKnowledgeOptions = schemaKnowledgeOptions.Value;
    }

    /// <summary>
    /// Searches the JSON knowledge index and returns the highest-scoring schema documents for the supplied question.
    /// </summary>
    /// <param name="question">The natural-language question used to score indexed schema documents.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the file read.</param>
    /// <returns>A collection of the most relevant schema knowledge documents.</returns>
    public async Task<IReadOnlyCollection<SchemaKnowledgeDocument>> SearchAsync(string question, CancellationToken cancellationToken)
    {
        if (File.Exists(_schemaKnowledgeOptions.FilePath) is false)
        {
            return [];
        }

        await using var stream = File.OpenRead(_schemaKnowledgeOptions.FilePath);
        var artifact = await JsonSerializer.DeserializeAsync<SchemaKnowledgeArtifact>(
            stream,
            JsonSerializerOptions,
            cancellationToken);

        var documents = artifact?.Documents?.ToArray();

        if (documents is null || documents.Length == 0)
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

    /// <summary>
    /// Computes a naive relevance score based on keyword matches against the indexed schema document.
    /// </summary>
    /// <param name="document">The indexed schema document being evaluated.</param>
    /// <param name="keywords">The extracted keywords from the user question.</param>
    /// <returns>An integer score used to rank the document.</returns>
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

            if (document.Keywords.Any(documentKeyword => documentKeyword.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                score += 5;
            }

            if (document.TableNames.Any(tableName => tableName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                score += 4;
            }
        }

        return score;
    }
}
