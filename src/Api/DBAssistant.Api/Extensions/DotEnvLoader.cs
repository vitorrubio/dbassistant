namespace DBAssistant.Api.Extensions;

/// <summary>
/// Loads environment variables from the repository dotenv file before the host is built.
/// </summary>
public static class DotEnvLoader
{
    /// <summary>
    /// Loads the dotenv file from the repository root into the current process environment.
    /// </summary>
    public static void LoadFromRepositoryRoot()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var envPath = Path.Combine(repositoryRoot, ".env");

        if (File.Exists(envPath) is false)
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(envPath))
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

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DBAssistant.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate the repository root from the API application.");
    }
}
