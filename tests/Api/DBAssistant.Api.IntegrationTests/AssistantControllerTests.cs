using System.Net;
using System.Net.Http.Json;
using System.Text;
using DBAssistant.UseCases.Models;
using DBAssistant.UseCases.Ports;
using DBAssistant.UseCases.Exceptions;
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
    [Trait("Category", "IntegrationTests")]
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
    /// Ensures omitted optional flags are accepted by model binding and still return HTTP 200.
    /// </summary>
    [Fact]
    [Trait("Category", "IntegrationTests")]
    public async Task QueryAsync_ShouldAcceptOmittedOptionalFlags()
    {
        var client = CreateClient();
        var response = await SendRawQueryAsync(
            client,
            """
            {
              "question": "List orders"
            }
            """,
            includeApiKeyHeader: true);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().NotContain("\"sql\"");
        payload.Should().NotContain("\"explanation\"");
        payload.Should().NotContain("\"executed\"");
        payload.Should().NotContain("\"schemaContextSource\"");
    }

    /// <summary>
    /// Ensures explicit optional flags are accepted by model binding and still return HTTP 200.
    /// </summary>
    [Fact]
    [Trait("Category", "IntegrationTests")]
    public async Task QueryAsync_ShouldAcceptExplicitOptionalFlags()
    {
        var client = CreateClient();
        var response = await SendRawQueryAsync(
            client,
            """
            {
              "question": "List orders",
              "executeSql": false,
              "showDetails": true
            }
            """,
            includeApiKeyHeader: true);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("\"sql\":\"SELECT * FROM Orders\"");
        payload.Should().Contain("\"explanation\":\"Fake response\"");
        payload.Should().Contain("\"executed\":false");
    }

    /// <summary>
    /// Ensures the query endpoint rejects requests that do not provide the configured API key.
    /// </summary>
    [Fact]
    [Trait("Category", "IntegrationTests")]
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

    /// <summary>
    /// Ensures execution failures caused by invalid generated SQL are surfaced as HTTP 400 instead of empty results.
    /// </summary>
    [Fact]
    [Trait("Category", "IntegrationTests")]
    public async Task QueryAsync_ShouldReturnBadRequest_WhenGeneratedSqlCannotBeExecuted()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IProcessNaturalLanguageQueryUseCase, FakeFailingProcessNaturalLanguageQueryUseCase>();
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add(GetApiKeyHeaderName(), GetApiKeyHeaderValue());

        var response = await SendQueryAsync(
            client,
            new QueryAssistantRequest
            {
                Question = "List orders"
            },
            includeApiKeyHeader: true);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("produced database warnings");
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

    private static Task<HttpResponseMessage> SendRawQueryAsync(
        HttpClient client,
        string jsonPayload,
        bool includeApiKeyHeader)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/query")
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
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
            var showDetails = request.ShowDetails ?? false;

            return Task.FromResult(new QueryAssistantResponse
            {
                Sql = showDetails ? "SELECT * FROM Orders" : null,
                Explanation = showDetails ? "Fake response" : null,
                SchemaContextSource = showDetails ? "information_schema" : null,
                Executed = showDetails ? request.ExecuteSql ?? true : null,
                ResultsAsText = "Fake summary"
            });
        }
    }

    /// <summary>
    /// Simulates a use case failure caused by invalid generated SQL.
    /// </summary>
    private sealed class FakeFailingProcessNaturalLanguageQueryUseCase : IProcessNaturalLanguageQueryUseCase
    {
        /// <inheritdoc />
        public Task<QueryAssistantResponse> ExecuteAsync(QueryAssistantRequest request, CancellationToken cancellationToken)
        {
            throw new QueryExecutionException("The generated SQL produced database warnings and was rejected: Warning 1411: Incorrect datetime value.");
        }
    }
}
