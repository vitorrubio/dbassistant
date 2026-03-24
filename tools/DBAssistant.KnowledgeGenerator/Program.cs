using DBAssistant.KnowledgeGenerator;

var repositoryRoot = ResolveRepositoryRoot();
var envPath = Path.Combine(repositoryRoot, ".env");
var outputPath = Path.Combine(repositoryRoot, "knowledge", "schema-index.json");
var environmentValues = EnvFileReader.Read(envPath);

var options = new KnowledgeGenerationOptions
{
    Host = GetRequiredValue(environmentValues, "MYSQL_HOST"),
    Port = int.Parse(GetRequiredValue(environmentValues, "MYSQL_PORT")),
    Database = GetRequiredValue(environmentValues, "MYSQL_DATABASE"),
    Username = GetRequiredValue(environmentValues, "MYSQL_USERNAME"),
    Password = GetRequiredValue(environmentValues, "MYSQL_PASSWORD"),
    OutputPath = outputPath
};

var generator = new SchemaKnowledgeGenerator();
await generator.GenerateAsync(options, CancellationToken.None);

Console.WriteLine($"Knowledge artifact generated at {outputPath}");

static string ResolveRepositoryRoot()
{
    var currentDirectory = AppContext.BaseDirectory;
    var directory = new DirectoryInfo(currentDirectory);

    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "DBAssistant.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Unable to locate the repository root from the tool execution directory.");
}

static string GetRequiredValue(IReadOnlyDictionary<string, string> values, string key)
{
    if (values.TryGetValue(key, out var value) && string.IsNullOrWhiteSpace(value) is false)
    {
        return value;
    }

    throw new InvalidOperationException($"Missing required environment variable '{key}' in .env.");
}
