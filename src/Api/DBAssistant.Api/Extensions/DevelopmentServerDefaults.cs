namespace DBAssistant.Api.Extensions;

/// <summary>
/// Applies developer-friendly defaults for HTTPS and external access when the API runs in local debug scenarios.
/// </summary>
public static class DevelopmentServerDefaults
{
    private const string DEFAULT_HTTPS_URL = "https://0.0.0.0:7043";
    private const string DEFAULT_CERTIFICATE_PATH = "/tmp/dbassistant-devcert.pfx";
    private const string DEFAULT_CERTIFICATE_PASSWORD = "DBAssistantDevCert123!";

    /// <summary>
    /// Applies default environment variables for local HTTPS hosting when they were not provided explicitly.
    /// </summary>
    public static void Apply()
    {
        SetIfMissing("ASPNETCORE_URLS", DEFAULT_HTTPS_URL);

        if (File.Exists(DEFAULT_CERTIFICATE_PATH))
        {
            SetIfMissing("Kestrel__Certificates__Default__Path", DEFAULT_CERTIFICATE_PATH);
            SetIfMissing("Kestrel__Certificates__Default__Password", DEFAULT_CERTIFICATE_PASSWORD);
        }
    }

    private static void SetIfMissing(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
