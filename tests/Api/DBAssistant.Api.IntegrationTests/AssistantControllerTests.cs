using System.Net;
using System.Net.Http.Json;
using DBAssistant.Api.Configuration;
using DBAssistant.UseCases.Abstractions;
using DBAssistant.UseCases.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DBAssistant.Api.IntegrationTests;

public sealed class AssistantControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AssistantControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task QueryAsync_ShouldReturnOk()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IProcessNaturalLanguageQueryUseCase, FakeProcessNaturalLanguageQueryUseCase>();
            });
        }).CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/assistant/query",
            new QueryAssistantRequest
            {
                Question = "List orders",
                ExecuteSql = false
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class FakeProcessNaturalLanguageQueryUseCase : IProcessNaturalLanguageQueryUseCase
    {
        public Task<NaturalLanguageQueryResponse> ExecuteAsync(
            NaturalLanguageQueryRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new NaturalLanguageQueryResponse
            {
                Sql = "SELECT * FROM Orders",
                Explanation = "Fake response",
                SchemaContextSource = "information_schema",
                Executed = false
            });
        }
    }
}
