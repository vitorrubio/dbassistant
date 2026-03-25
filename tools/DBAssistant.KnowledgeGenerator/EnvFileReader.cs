namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Reads key-value pairs from a dotenv-style file.
/// </summary>
public static class EnvFileReader
{
    /// <summary>
    /// Loads variables from the specified dotenv file path.
    /// </summary>
    /// <param name="filePath">The dotenv file path to read.</param>
    /// <returns>A case-insensitive dictionary containing the parsed environment variables.</returns>
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

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            values[key] = value;
        }

        return values;
    }

    /// <summary>
    /// Loads variables from the specified dotenv file path when the file exists.
    /// </summary>
    /// <param name="filePath">The dotenv file path to read.</param>
    /// <returns>A case-insensitive dictionary containing the parsed environment variables, or an empty dictionary.</returns>
    public static IReadOnlyDictionary<string, string> ReadOptional(string filePath)
    {
        return File.Exists(filePath)
            ? Read(filePath)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
