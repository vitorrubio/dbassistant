namespace DBAssistant.KnowledgeGenerator;

/// <summary>
/// Stores the options required to connect to the source database and write the knowledge artifact.
/// </summary>
public sealed class KnowledgeGenerationOptions
{
    /// <summary>
    /// Gets or sets the MySQL host name.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MySQL port number.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the target database name.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user name used to connect to the source database.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password used to connect to the source database.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output directory where runtime RAG artifacts are written.
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON artifact path that stores the generated schema documents.
    /// </summary>
    public string SchemaDocumentsPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSONL artifact path that stores one embedding input per document.
    /// </summary>
    public string EmbeddingInputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON artifact path that stores the generated schema embeddings.
    /// </summary>
    public string EmbeddingsPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OpenAI API key used to precompute schema embeddings.
    /// </summary>
    public string OpenAiApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL of the OpenAI-compatible API endpoint.
    /// </summary>
    public string OpenAiBaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// Gets or sets the model identifier used to generate schema embeddings.
    /// </summary>
    public string OpenAiEmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Builds the runtime MySQL connection string from the configured parts.
    /// </summary>
    /// <returns>A MySQL connection string.</returns>
    public string BuildConnectionString()
    {
        return $"Server={Host};Port={Port};Database={Database};User ID={Username};Password={Password};";
    }

    /// <summary>
    /// Builds a MySQL connection string that connects to the server without selecting a default database.
    /// </summary>
    /// <returns>A server-level MySQL connection string.</returns>
    public string BuildServerConnectionString()
    {
        return $"Server={Host};Port={Port};User ID={Username};Password={Password};";
    }

    /// <summary>
    /// Determines whether runtime schema embeddings can be generated from the configured OpenAI settings.
    /// </summary>
    /// <returns><see langword="true"/> when embedding generation is configured; otherwise, <see langword="false"/>.</returns>
    public bool CanGenerateEmbeddings()
    {
        return string.IsNullOrWhiteSpace(OpenAiApiKey) is false &&
               string.IsNullOrWhiteSpace(OpenAiBaseUrl) is false &&
               string.IsNullOrWhiteSpace(OpenAiEmbeddingModel) is false &&
               string.IsNullOrWhiteSpace(EmbeddingsPath) is false;
    }
}
