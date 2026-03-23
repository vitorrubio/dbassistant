namespace DBAssistant.Api.IntegrationTests.Acceptance;

/// <summary>
/// Reads key-value pairs from the repository dotenv file for the acceptance test setup.
/// </summary>
public static class AcceptanceEnvFileReader
{
    /// <summary>
    /// Loads a dotenv file into a case-insensitive dictionary.
    /// </summary>
    /// <param name="filePath">The dotenv file path.</param>
    /// <returns>The parsed key-value pairs.</returns>
    public static IReadOnlyDictionary<string, string> Read(string filePath)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');

            if (separatorIndex <= 0)
            {
                continue;
            }

            values[line[..separatorIndex].Trim()] = line[(separatorIndex + 1)..].Trim();
        }

        return values;
    }
}
