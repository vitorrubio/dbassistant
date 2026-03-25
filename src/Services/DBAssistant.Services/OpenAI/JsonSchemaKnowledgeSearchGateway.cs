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

        SchemaKnowledgeEmbeddingsArtifact? embeddingsArtifact = null;
        IReadOnlyCollection<float>? queryEmbedding = null;

        try
        {
            embeddingsArtifact = await LoadOrBuildEmbeddingsArtifactAsync(artifact, cancellationToken);
            queryEmbedding = await _openAiTransportClient.CreateEmbeddingAsync(question, cancellationToken);
        }
        catch (Exception exception) when (exception is DBAssistant.UseCases.Exceptions.ExternalServiceUnavailableException or IOException or UnauthorizedAccessException)
        {
            queryEmbedding = null;
            embeddingsArtifact = null;
        }

        var embeddingsById = embeddingsArtifact?.Documents.ToDictionary(item => item.Id, StringComparer.Ordinal)
            ?? new Dictionary<string, SchemaKnowledgeEmbeddingDocument>(StringComparer.Ordinal);
        var normalizedQueryTerms = SplitTerms(question).ToArray();

        var documents = artifact.Documents
            .Select(document => new
            {
                Document = document,
                Score = CalculateCombinedScore(document, normalizedQueryTerms, queryEmbedding, embeddingsById)
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
            var embedding = await _openAiTransportClient.CreateEmbeddingAsync(document.EmbeddingInput, cancellationToken);
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

    private static double CalculateCombinedScore(
        SchemaKnowledgeDocument document,
        IReadOnlyCollection<string> normalizedQueryTerms,
        IReadOnlyCollection<float>? queryEmbedding,
        IReadOnlyDictionary<string, SchemaKnowledgeEmbeddingDocument> embeddingsById)
    {
        var lexicalScore = CalculateLexicalScore(document, normalizedQueryTerms);

        if (queryEmbedding is null ||
            embeddingsById.TryGetValue(document.Id, out var embeddingDocument) is false)
        {
            return lexicalScore;
        }

        var semanticScore = CosineSimilarity(queryEmbedding, embeddingDocument.Embedding);

        if (semanticScore <= 0)
        {
            return lexicalScore;
        }

        return (semanticScore * 0.7d) + (lexicalScore * 0.3d);
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

    private static double CalculateLexicalScore(SchemaKnowledgeDocument document, IReadOnlyCollection<string> normalizedQueryTerms)
    {
        if (normalizedQueryTerms.Count == 0)
        {
            return 0;
        }

        var lexicalTerms = document.Keywords
            .Concat(document.SemanticTags)
            .Concat(document.QuestionPatterns)
            .Concat(document.JoinHints)
            .Concat(document.GetAllTableNames())
            .SelectMany(SplitTerms)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (lexicalTerms.Count == 0)
        {
            return 0;
        }

        var matches = normalizedQueryTerms.Count(lexicalTerms.Contains);
        return matches == 0
            ? 0
            : (double)matches / normalizedQueryTerms.Count;
    }

    private static IEnumerable<string> SplitTerms(string value)
    {
        return value
            .Replace("_", " ", StringComparison.Ordinal)
            .Split([' ', '-', '.', ',', ':', ';', '\n', '\r', '(', ')'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length >= 2)
            .Select(term => term.ToLowerInvariant());
    }
}
