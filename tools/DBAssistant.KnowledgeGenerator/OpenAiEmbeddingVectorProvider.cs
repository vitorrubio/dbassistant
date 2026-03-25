using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Produces embedding vectors for schema documents.
/// </summary>
public interface IEmbeddingVectorProvider
{
    /// <summary>
    /// Creates embedding vectors for the provided input texts.
    /// </summary>
    /// <param name="options">The generation options that contain the embedding configuration.</param>
    /// <param name="inputs">The ordered text inputs to embed.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the operation.</param>
    /// <returns>The ordered embedding vectors.</returns>
    Task<IReadOnlyList<IReadOnlyCollection<float>>> CreateEmbeddingsAsync(
        KnowledgeGenerationOptions options,
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken);
}

/// <summary>
/// Generates embedding vectors for schema documents using the configured OpenAI-compatible endpoint.
/// </summary>
public sealed class OpenAiEmbeddingVectorProvider : IEmbeddingVectorProvider
{
    private const int MAX_BATCH_SIZE = 32;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Creates embeddings for the provided input texts in batches.
    /// </summary>
    /// <param name="options">The generation options that contain the OpenAI configuration.</param>
    /// <param name="inputs">The ordered text inputs to embed.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the operation.</param>
    /// <returns>The ordered embedding vectors.</returns>
    public async Task<IReadOnlyList<IReadOnlyCollection<float>>> CreateEmbeddingsAsync(
        KnowledgeGenerationOptions options,
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken)
    {
        if (inputs.Count == 0)
        {
            return [];
        }

        using var httpClient = CreateHttpClient(options);
        var vectors = new List<IReadOnlyCollection<float>>(inputs.Count);

        foreach (var batch in Batch(inputs, MAX_BATCH_SIZE))
        {
            using var response = await httpClient.PostAsJsonAsync(
                "embeddings",
                new
                {
                    model = options.OpenAiEmbeddingModel,
                    input = batch
                },
                cancellationToken);

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode is false)
            {
                throw new InvalidOperationException(
                    $"OpenAI embedding request failed with status code {(int)response.StatusCode}: {payload}");
            }

            var typedResponse = JsonSerializer.Deserialize<OpenAiEmbeddingsResponse>(payload, JsonSerializerOptions);
            var responseVectors = typedResponse?.Data
                .OrderBy(item => item.Index)
                .Select(item => (IReadOnlyCollection<float>)item.Embedding)
                .ToArray();

            if (responseVectors is null || responseVectors.Length != batch.Count)
            {
                throw new InvalidOperationException("OpenAI returned an invalid batched embedding payload.");
            }

            vectors.AddRange(responseVectors);
        }

        return vectors;
    }

    private static HttpClient CreateHttpClient(KnowledgeGenerationOptions options)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.OpenAiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(90)
        };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.OpenAiApiKey);
        return httpClient;
    }

    private static IEnumerable<IReadOnlyList<string>> Batch(IReadOnlyList<string> inputs, int batchSize)
    {
        for (var index = 0; index < inputs.Count; index += batchSize)
        {
            var currentBatchSize = Math.Min(batchSize, inputs.Count - index);
            var batch = new List<string>(currentBatchSize);

            for (var offset = 0; offset < currentBatchSize; offset++)
            {
                batch.Add(inputs[index + offset]);
            }

            yield return batch;
        }
    }

    private sealed class OpenAiEmbeddingsResponse
    {
        public IReadOnlyCollection<OpenAiEmbeddingItem> Data { get; init; } = [];
    }

    private sealed class OpenAiEmbeddingItem
    {
        public int Index { get; init; }

        public IReadOnlyCollection<float> Embedding { get; init; } = [];
    }
}
