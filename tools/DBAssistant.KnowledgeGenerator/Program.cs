using DBAssistant.KnowledgeGenerator;

var executionRoot = ResolveExecutionRoot();
var envPath = Path.Combine(executionRoot, ".env");
var environmentValues = EnvFileReader.ReadOptional(envPath);
var outputDirectory = GetOptionalValue(environmentValues, "SCHEMA_KNOWLEDGE_DIRECTORY")
    ?? Path.Combine(executionRoot, "knowledge", "runtime");
var schemaDocumentsPath = GetOptionalValue(environmentValues, "SCHEMA_KNOWLEDGE_FILE_PATH")
    ?? Path.Combine(outputDirectory, "schema-documents.json");
var embeddingInputPath = GetOptionalValue(environmentValues, "SCHEMA_KNOWLEDGE_EMBEDDING_INPUT_FILE_PATH")
    ?? Path.Combine(outputDirectory, "schema-embedding-input.jsonl");

var options = new KnowledgeGenerationOptions
{
    Host = GetRequiredValue(environmentValues, "MYSQL_HOST"),
    Port = int.Parse(GetRequiredValue(environmentValues, "MYSQL_PORT")),
    Database = GetRequiredValue(environmentValues, "MYSQL_DATABASE"),
    Username = GetRequiredValue(environmentValues, "MYSQL_USERNAME"),
    Password = GetRequiredValue(environmentValues, "MYSQL_PASSWORD"),
    OutputDirectory = ResolveAbsolutePath(executionRoot, outputDirectory),
    SchemaDocumentsPath = ResolveAbsolutePath(executionRoot, schemaDocumentsPath),
    EmbeddingInputPath = ResolveAbsolutePath(executionRoot, embeddingInputPath)
};

var generator = new SchemaKnowledgeGenerator();
var result = await generator.GenerateAsync(options, CancellationToken.None);

Console.WriteLine($"Schema documents generated at {result.SchemaDocumentsPath}");
Console.WriteLine($"Embedding input generated at {result.EmbeddingInputPath}");

static string ResolveExecutionRoot()
{
    var currentDirectory = Directory.GetCurrentDirectory();
    var directory = new DirectoryInfo(currentDirectory);

    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "DBAssistant.sln")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return currentDirectory;
}

static string GetRequiredValue(IReadOnlyDictionary<string, string> values, string key)
{
    var environmentValue = Environment.GetEnvironmentVariable(key);

    if (string.IsNullOrWhiteSpace(environmentValue) is false)
    {
        return environmentValue.Trim();
    }

    if (values.TryGetValue(key, out var value) && string.IsNullOrWhiteSpace(value) is false)
    {
        return value;
    }

    throw new InvalidOperationException($"Missing required environment variable '{key}' in process environment or .env.");
}

static string? GetOptionalValue(IReadOnlyDictionary<string, string> values, string key)
{
    var environmentValue = Environment.GetEnvironmentVariable(key);

    if (string.IsNullOrWhiteSpace(environmentValue) is false)
    {
        return environmentValue.Trim();
    }

    return values.TryGetValue(key, out var value) && string.IsNullOrWhiteSpace(value) is false
        ? value
        : null;
}

static string ResolveAbsolutePath(string executionRoot, string path)
{
    return Path.IsPathRooted(path)
        ? path
        : Path.GetFullPath(Path.Combine(executionRoot, path));
}
