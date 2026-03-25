namespace DBAssistant.KnowledgeGenerator;

using System.Text.Json;

/// <summary>
/// Writes the generated schema knowledge artifacts to disk.
/// </summary>
public sealed class SchemaKnowledgeArtifactWriter
{
    private static readonly JsonSerializerOptions ArtifactJsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private static readonly JsonSerializerOptions JsonlSerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Writes the schema documents JSON and embedding input JSONL files.
    /// </summary>
    /// <param name="options">The generation options that define the output paths.</param>
    /// <param name="artifact">The schema document artifact.</param>
    /// <param name="embeddingRecords">The JSONL embedding records.</param>
    /// <param name="cancellationToken">The cancellation token used to stop the write operation.</param>
    /// <returns>The generated artifact paths.</returns>
    public async Task<KnowledgeGenerationResult> WriteAsync(
        KnowledgeGenerationOptions options,
        SchemaKnowledgeArtifact artifact,
        IReadOnlyCollection<SchemaEmbeddingInputRecord> embeddingRecords,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(options.OutputDirectory);

        await File.WriteAllTextAsync(
            options.SchemaDocumentsPath,
            JsonSerializer.Serialize(artifact, ArtifactJsonSerializerOptions),
            cancellationToken);

        var embeddingJsonl = string.Join(
            Environment.NewLine,
            embeddingRecords.Select(record => JsonSerializer.Serialize(record, JsonlSerializerOptions)));

        await File.WriteAllTextAsync(
            options.EmbeddingInputPath,
            embeddingJsonl + Environment.NewLine,
            cancellationToken);

        return new KnowledgeGenerationResult
        {
            SchemaDocumentsPath = options.SchemaDocumentsPath,
            EmbeddingInputPath = options.EmbeddingInputPath
        };
    }
}
