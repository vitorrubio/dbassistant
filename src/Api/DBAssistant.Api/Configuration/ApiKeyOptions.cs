namespace DBAssistant.Api.Configuration;

/// <summary>
/// Stores the API key configuration used to protect the HTTP endpoints.
/// </summary>
public sealed class ApiKeyOptions
{
    /// <summary>
    /// The default header name expected by the API.
    /// </summary>
    public const string DefaultHeaderName = "apiKey";

    /// <summary>
    /// The environment-variable name that defines the header name.
    /// </summary>
    public const string HeaderNameConfigurationKey = "API_KEY_HEADER_NAME";

    /// <summary>
    /// The environment-variable name that defines the expected key value.
    /// </summary>
    public const string HeaderValueConfigurationKey = "API_KEY_HEADER_VALUE";

    /// <summary>
    /// Gets or sets the request-header name used to carry the API key.
    /// </summary>
    public string HeaderName { get; set; } = DefaultHeaderName;

    /// <summary>
    /// Gets or sets the expected API key value.
    /// </summary>
    public string HeaderValue { get; set; } = string.Empty;

    /// <summary>
    /// Determines whether the API key configuration is complete and non-placeholder.
    /// </summary>
    /// <returns><see langword="true"/> when the API key configuration is usable; otherwise, <see langword="false"/>.</returns>
    public bool IsConfigured()
    {
        return string.IsNullOrWhiteSpace(HeaderName) is false &&
               string.IsNullOrWhiteSpace(HeaderValue) is false &&
               HeaderValue.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) is false &&
               HeaderValue.Contains("placeholder", StringComparison.OrdinalIgnoreCase) is false;
    }
}
