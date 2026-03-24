using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DBAssistant.Services.Configuration;
using DBAssistant.Services.OpenAI;
using DBAssistant.Services.SchemaKnowledge;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DBAssistant.Services.SchemaKnowledge;

/// <summary>
/// Provides vector-based retrieval over the local schema knowledge artifact using OpenAI embeddings plus a persisted local cache.
/// </summary>
public sealed class JsonSchemaKnowledgeSearchGateway : ISchemaKnowledgeSearchGateway
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IMemoryCache _memoryCache;
    private readonly OpenAiTransportClient _openAiTransportClient;
    private readonly OpenAiOptions _openAiOptions;
    private readonly CacheOptions _cacheOptions;
    private readonly SchemaKnowledgeOptions _schemaKnowledgeOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSchemaKnowledgeSearchGateway"/> class.
    /// </summary>
    public JsonSchemaKnowledgeSearchGateway(
        IMemoryCache memoryCache,
        OpenAiTransportClient openAiTransportClient,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<CacheOptions> cacheOptions,
        IOptions<SchemaKnowledgeOptions> schemaKnowledgeOptions)
    {
        _memoryCache = memoryCache;
        _openAiTransportClient = openAiTransportClient;
        _openAiOptions = openAiOptions.Value;
        _cacheOptions = cacheOptions.Value;
        _schemaKnowledgeOptions = schemaKnowledgeOptions.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SchemaKnowledgeDocument>> SearchAsync(string question, CancellationToken cancellationToken)
    {
        if (File.Exists(_schemaKnowledgeOptions.FilePath) is false)
        {
            return [];
        }

        var cacheKey = $"schema-search::{Normalize(question)}";

        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyCollection<SchemaKnowledgeDocument>? cachedDocuments))
        {
            return cachedDocuments ?? [];
        }

        var artifact = await LoadKnowledgeArtifactAsync(cancellationToken);

        if (artifact.Documents.Count == 0)
        {
            return [];
        }

        var embeddingsArtifact = await LoadOrBuildEmbeddingsArtifactAsync(artifact, cancellationToken);
        var queryEmbedding = await _openAiTransportClient.CreateEmbeddingAsync(question, cancellationToken);
        var embeddingsById = embeddingsArtifact.Documents.ToDictionary(item => item.Id, StringComparer.Ordinal);

        var documents = artifact.Documents
            .Select(document => new
            {
                Document = document,
                Score = embeddingsById.TryGetValue(document.Id, out var embeddingDocument)
                    ? CosineSimilarity(queryEmbedding, embeddingDocument.Embedding)
                    : double.MinValue
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .Take(Math.Max(1, _schemaKnowledgeOptions.MaxDocuments))
            .Select(item => item.Document)
            .ToArray();

        _memoryCache.Set(
            cacheKey,
            documents,
            TimeSpan.FromMinutes(Math.Max(1, _cacheOptions.SchemaSearchMinutes)));

        return documents;
    }

    private async Task<SchemaKnowledgeArtifact> LoadKnowledgeArtifactAsync(CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(_schemaKnowledgeOptions.FilePath);
        var artifact = await JsonSerializer.DeserializeAsync<SchemaKnowledgeArtifact>(stream, JsonSerializerOptions, cancellationToken);
        return artifact ?? new SchemaKnowledgeArtifact();
    }

    private async Task<SchemaKnowledgeEmbeddingsArtifact> LoadOrBuildEmbeddingsArtifactAsync(
        SchemaKnowledgeArtifact artifact,
        CancellationToken cancellationToken)
    {
        if (File.Exists(_schemaKnowledgeOptions.EmbeddingsFilePath))
        {
            await using var stream = File.OpenRead(_schemaKnowledgeOptions.EmbeddingsFilePath);
            var existingArtifact = await JsonSerializer.DeserializeAsync<SchemaKnowledgeEmbeddingsArtifact>(stream, JsonSerializerOptions, cancellationToken);

            if (existingArtifact is not null &&
                string.Equals(existingArtifact.FormatVersion, artifact.FormatVersion, StringComparison.Ordinal) &&
                existingArtifact.KnowledgeGeneratedAtUtc == artifact.GeneratedAtUtc &&
                string.Equals(existingArtifact.EmbeddingModel, _openAiOptions.EmbeddingModel, StringComparison.Ordinal))
            {
                return existingArtifact;
            }
        }

        var embeddedDocuments = new List<SchemaKnowledgeEmbeddingDocument>();

        foreach (var document in artifact.Documents)
        {
            var embedding = await _openAiTransportClient.CreateEmbeddingAsync(BuildEmbeddingInput(document), cancellationToken);
            embeddedDocuments.Add(new SchemaKnowledgeEmbeddingDocument
            {
                Id = document.Id,
                Embedding = embedding.ToArray()
            });
        }

        var newArtifact = new SchemaKnowledgeEmbeddingsArtifact
        {
            FormatVersion = artifact.FormatVersion,
            KnowledgeGeneratedAtUtc = artifact.GeneratedAtUtc,
            EmbeddingModel = _openAiOptions.EmbeddingModel,
            Documents = embeddedDocuments
        };

        var embeddingsDirectory = Path.GetDirectoryName(_schemaKnowledgeOptions.EmbeddingsFilePath);

        if (string.IsNullOrWhiteSpace(embeddingsDirectory) is false)
        {
            Directory.CreateDirectory(embeddingsDirectory);
        }

        await File.WriteAllTextAsync(
            _schemaKnowledgeOptions.EmbeddingsFilePath,
            JsonSerializer.Serialize(newArtifact, JsonSerializerOptions),
            cancellationToken);

        return newArtifact;
    }

    private static string BuildEmbeddingInput(SchemaKnowledgeDocument document)
    {
        return $"{document.Title}\n{document.Content}\nKeywords: {string.Join(", ", document.Keywords)}";
    }

    private static double CosineSimilarity(IReadOnlyCollection<float> left, IReadOnlyCollection<float> right)
    {
        if (left.Count == 0 || right.Count == 0 || left.Count != right.Count)
        {
            return double.MinValue;
        }

        var leftArray = left as float[] ?? left.ToArray();
        var rightArray = right as float[] ?? right.ToArray();

        double dot = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (var index = 0; index < leftArray.Length; index++)
        {
            dot += leftArray[index] * rightArray[index];
            leftMagnitude += leftArray[index] * leftArray[index];
            rightMagnitude += rightArray[index] * rightArray[index];
        }

        if (leftMagnitude <= 0 || rightMagnitude <= 0)
        {
            return double.MinValue;
        }

        return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }

    private static string Normalize(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes);
    }
}
