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

        if (context.Request.Headers.TryGetValue(_options.HeaderName, out var providedHeaderValues) is false ||
            string.IsNullOrWhiteSpace(providedHeaderValues.ToString()) ||
            MatchesExpectedValue(providedHeaderValues.ToString()) is false)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = $"Provide the '{_options.HeaderName}' header with a valid API key."
            });

            return;
        }

        await _next(context);
    }

    private bool MatchesExpectedValue(string providedValue)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedValue);
        var expectedBytes = Encoding.UTF8.GetBytes(_options.HeaderValue);

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
