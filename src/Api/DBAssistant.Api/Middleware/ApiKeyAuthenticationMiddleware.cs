using System.Security.Cryptography;
using System.Text;
using DBAssistant.Api.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace DBAssistant.Api.Middleware;

/// <summary>
/// Enforces API key authentication for the REST endpoints exposed by the application.
/// </summary>
public sealed class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiKeyOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyAuthenticationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The configured API key options.</param>
    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ApiKeyOptions options)
    {
        _next = next;
        _options = options;
    }

    /// <summary>
    /// Validates the configured API key before requests reach the protected API routes.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task that completes when the middleware finishes handling the request.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) is false)
        {
            await _next(context);
            return;
        }

        var providedValue = ResolveProvidedApiKey(context.Request);

        if (string.IsNullOrWhiteSpace(providedValue) || MatchesExpectedValue(providedValue) is false)
        {
            var observedHeaders = string.Join(
                ", ",
                [
                    $"{_options.HeaderName}={RequestContainsHeader(context.Request, _options.HeaderName)}",
                    $"apiKey={RequestContainsHeader(context.Request, "apiKey")}",
                    $"x-api-key={RequestContainsHeader(context.Request, "x-api-key")}",
                    $"authorization={RequestContainsHeader(context.Request, "Authorization")}"
                ]);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = $"Provide the '{_options.HeaderName}' header with a valid API key. Observed headers: {observedHeaders}."
            });

            return;
        }

        await _next(context);
    }

    private bool MatchesExpectedValue(string providedValue)
    {
        var normalizedProvidedValue = providedValue.Trim();
        var normalizedExpectedValue = _options.HeaderValue.Trim();
        var providedBytes = Encoding.UTF8.GetBytes(normalizedProvidedValue);
        var expectedBytes = Encoding.UTF8.GetBytes(normalizedExpectedValue);

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    private string? ResolveProvidedApiKey(HttpRequest request)
    {
        if (TryReadHeaderValue(request, _options.HeaderName, out var configuredHeaderValue))
        {
            return configuredHeaderValue;
        }

        if (TryReadHeaderValue(request, "apiKey", out var legacyHeaderValue))
        {
            return legacyHeaderValue;
        }

        if (TryReadHeaderValue(request, "x-api-key", out var standardHeaderValue))
        {
            return standardHeaderValue;
        }

        if (request.Headers.Authorization.ToString() is { Length: > 7 } authorizationHeaderValue &&
            authorizationHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authorizationHeaderValue["Bearer ".Length..];
        }

        return null;
    }

    private static bool TryReadHeaderValue(HttpRequest request, string headerName, out string? value)
    {
        if (request.Headers.TryGetValue(headerName, out var headerValues) &&
            string.IsNullOrWhiteSpace(headerValues.ToString()) is false)
        {
            value = headerValues.ToString();
            return true;
        }

        value = null;
        return false;
    }

    private static bool RequestContainsHeader(HttpRequest request, string headerName)
    {
        return request.Headers.TryGetValue(headerName, out var headerValues) &&
               string.IsNullOrWhiteSpace(headerValues.ToString()) is false;
    }
}
