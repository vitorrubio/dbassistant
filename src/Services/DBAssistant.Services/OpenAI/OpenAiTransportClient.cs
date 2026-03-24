using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DBAssistant.Services.Configuration;
using DBAssistant.Services.OpenAI.Contracts;
using DBAssistant.UseCases.Exceptions;
using Microsoft.Extensions.Options;

namespace DBAssistant.Services.OpenAI;

/// <summary>
/// Wraps typed access to the OpenAI Responses and Embeddings APIs.
/// </summary>
public sealed class OpenAiTransportClient
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _openAiOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiTransportClient"/> class.
    /// </summary>
    public OpenAiTransportClient(HttpClient httpClient, IOptions<OpenAiOptions> openAiOptions)
    {
        _httpClient = httpClient;
        _openAiOptions = openAiOptions.Value;
    }

    /// <summary>
    /// Sends a typed Responses API request and returns the typed envelope.
    /// </summary>
    public async Task<OpenAiResponsesResponse> CreateResponseAsync(OpenAiResponsesRequest requestPayload, CancellationToken cancellationToken)
    {
        ValidateConfiguration(requireEmbeddingModel: false);

        using var request = new HttpRequestMessage(HttpMethod.Post, "responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);
        request.Content = JsonContent.Create(requestPayload);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode is false)
        {
            throw new ExternalServiceUnavailableException($"OpenAI request failed with status code {(int)response.StatusCode}: {payload}");
        }

        var typed = JsonSerializer.Deserialize<OpenAiResponsesResponse>(payload, JsonSerializerOptions);

        if (typed is null)
        {
            throw new ExternalServiceUnavailableException("OpenAI returned an invalid Responses API payload.");
        }

        return typed;
    }

    /// <summary>
    /// Sends one text to the embeddings API and returns the vector.
    /// </summary>
    public async Task<IReadOnlyCollection<float>> CreateEmbeddingAsync(string input, CancellationToken cancellationToken)
    {
        ValidateConfiguration(requireEmbeddingModel: true);

        using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);
        request.Content = JsonContent.Create(new OpenAiEmbeddingsRequest
        {
            Model = _openAiOptions.EmbeddingModel,
            Input = input
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode is false)
        {
            throw new ExternalServiceUnavailableException($"OpenAI embedding request failed with status code {(int)response.StatusCode}: {payload}");
        }

        var typed = JsonSerializer.Deserialize<OpenAiEmbeddingsResponse>(payload, JsonSerializerOptions);
        var embedding = typed?.Data.FirstOrDefault()?.Embedding;

        if (embedding is null || embedding.Count == 0)
        {
            throw new ExternalServiceUnavailableException("OpenAI returned an invalid embedding payload.");
        }

        return embedding;
    }

    private void ValidateConfiguration(bool requireEmbeddingModel)
    {
        if (string.IsNullOrWhiteSpace(_openAiOptions.ApiKey) ||
            string.IsNullOrWhiteSpace(_openAiOptions.BaseUrl) ||
            string.IsNullOrWhiteSpace(_openAiOptions.Model) ||
            (requireEmbeddingModel && string.IsNullOrWhiteSpace(_openAiOptions.EmbeddingModel)))
        {
            throw new ExternalServiceUnavailableException("OpenAI configuration is incomplete. Fill the .env placeholders before using this endpoint.");
        }
    }
}
