using System.Net;
using System.Net.Http.Json;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DBAssistant.Api.IntegrationTests;

/// <summary>
/// Verifies the assistant API endpoint through an in-memory test server.
/// </summary>
public sealed class AssistantControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string DefaultApiKeyHeaderName = "x-api-key";
    private const string DefaultApiKeyHeaderValue = "test-api-key";
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantControllerTests"/> class.
    /// </summary>
    /// <param name="factory">The web application factory used to host the API in memory.</param>
    public AssistantControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Ensures the query endpoint returns HTTP 200 when the use case completes successfully.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration Tests")]
    public async Task QueryAsync_ShouldReturnOk()
    {
        var client = CreateClient();
        var response = await SendQueryAsync(
            client,
            new QueryAssistantRequest
            {
                Question = "List orders",
                ExecuteSql = false
            },
            includeApiKeyHeader: true);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Ensures the query endpoint rejects requests that do not provide the configured API key.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration Tests")]
    public async Task QueryAsync_ShouldReturnUnauthorized_WhenApiKeyIsMissing()
    {
        var client = CreateClient(includeApiKeyHeader: false);
        var response = await SendQueryAsync(
            client,
            new QueryAssistantRequest
            {
                Question = "List orders",
                ExecuteSql = false
            },
            includeApiKeyHeader: false);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateClient(bool includeApiKeyHeader = true)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IProcessNaturalLanguageQueryUseCase, FakeProcessNaturalLanguageQueryUseCase>();
            });
        }).CreateClient();

        if (includeApiKeyHeader)
        {
            client.DefaultRequestHeaders.Add(GetApiKeyHeaderName(), GetApiKeyHeaderValue());
        }

        return client;
    }

    private static Task<HttpResponseMessage> SendQueryAsync(
        HttpClient client,
        QueryAssistantRequest request,
        bool includeApiKeyHeader)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/query")
        {
            Content = JsonContent.Create(request)
        };

        if (includeApiKeyHeader)
        {
            message.Headers.Add(GetApiKeyHeaderName(), GetApiKeyHeaderValue());
        }

        return client.SendAsync(message);
    }

    private static string GetApiKeyHeaderName()
    {
        return Environment.GetEnvironmentVariable("API_KEY_HEADER_NAME") ?? DefaultApiKeyHeaderName;
    }

    private static string GetApiKeyHeaderValue()
    {
        return Environment.GetEnvironmentVariable("API_KEY_HEADER_VALUE") ?? DefaultApiKeyHeaderValue;
    }

    /// <summary>
    /// Supplies a deterministic use case implementation for the API integration test.
    /// </summary>
    private sealed class FakeProcessNaturalLanguageQueryUseCase : IProcessNaturalLanguageQueryUseCase
    {
        /// <summary>
        /// Returns a deterministic fake assistant response for the integration test.
        /// </summary>
        /// <param name="request">The natural-language query request received by the controller.</param>
        /// <param name="cancellationToken">The cancellation token used to stop the fake call.</param>
        /// <returns>A fake natural-language query response.</returns>
        public Task<QueryAssistantResponse> ExecuteAsync(
            QueryAssistantRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new QueryAssistantResponse
            {
                Sql = "SELECT * FROM Orders",
                Explanation = "Fake response",
                SchemaContextSource = "information_schema",
                Executed = false
            });
        }
    }
}
