using System.Text.Json;
using DBAssistant.KnowledgeGenerator;
using FluentAssertions;
using Xunit;

namespace DBAssistant.KnowledgeGenerator.UnitTests;

public sealed class SchemaKnowledgeArtifactWriterTests
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task WriteAsync_ShouldRespectConfiguredOutputPaths()
    {
        var temporaryRoot = Path.Combine(Path.GetTempPath(), $"dbassistant-knowledge-tests-{Guid.NewGuid():N}");
        var outputDirectory = Path.Combine(temporaryRoot, "custom-output");
        var options = new KnowledgeGenerationOptions
        {
            OutputDirectory = outputDirectory,
            SchemaDocumentsPath = Path.Combine(outputDirectory, "schema-documents.json"),
            EmbeddingInputPath = Path.Combine(outputDirectory, "schema-embedding-input.jsonl"),
            EmbeddingsPath = Path.Combine(outputDirectory, "schema-embeddings.json")
        };
        var artifact = new SchemaKnowledgeArtifact
        {
            DatabaseName = "Northwind",
            SchemaName = "northwind",
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            DocumentCount = 1,
            Documents =
            [
                new SchemaKnowledgeDocument
                {
                    Id = "table:orders",
                    DocType = "fact_table",
                    DatabaseName = "Northwind",
                    SchemaName = "northwind",
                    TableName = "orders",
                    RelatedTables = ["customers"],
                    Title = "orders fact table",
                    Content = "Orders content",
                    Keywords = ["orders"],
                    JoinHints = ["orders.customer_id = customers.id"],
                    QuestionPatterns = ["What is total orders by customer?"],
                    SemanticTags = ["sales"],
                    Source = "information_schema",
                    LastExtractedAtUtc = DateTimeOffset.UtcNow,
                    ContentHash = "ABC",
                    EmbeddingInput = "Orders embedding input",
                    TokenEstimate = 5
                }
            ]
        };
        var embeddingRecords = new[]
        {
            new SchemaEmbeddingInputRecord
            {
                Id = "table:orders",
                DocType = "fact_table",
                DatabaseName = "Northwind",
                SchemaName = "northwind",
                TableName = "orders",
                RelatedTables = ["customers"],
                Title = "orders fact table",
                EmbeddingInput = "Orders embedding input",
                ContentHash = "ABC",
                TokenEstimate = 5
            }
        };
        var embeddingsArtifact = new SchemaKnowledgeEmbeddingsArtifact
        {
            FormatVersion = "schema-rag-v2",
            KnowledgeGeneratedAtUtc = artifact.GeneratedAtUtc,
            EmbeddingModel = "text-embedding-3-small",
            Documents =
            [
                new SchemaKnowledgeEmbeddingDocument
                {
                    Id = "table:orders",
                    Embedding = [0.25f, 0.75f]
                }
            ]
        };

        try
        {
            var writer = new SchemaKnowledgeArtifactWriter();
            var result = await writer.WriteAsync(options, artifact, embeddingRecords, embeddingsArtifact, CancellationToken.None);

            result.SchemaDocumentsPath.Should().Be(options.SchemaDocumentsPath);
            result.EmbeddingInputPath.Should().Be(options.EmbeddingInputPath);
            result.EmbeddingsPath.Should().Be(options.EmbeddingsPath);
            File.Exists(options.SchemaDocumentsPath).Should().BeTrue();
            File.Exists(options.EmbeddingInputPath).Should().BeTrue();
            File.Exists(options.EmbeddingsPath).Should().BeTrue();

            var parsedArtifact = JsonSerializer.Deserialize<SchemaKnowledgeArtifact>(
                await File.ReadAllTextAsync(options.SchemaDocumentsPath),
                JsonSerializerOptions);
            parsedArtifact.Should().NotBeNull();
            parsedArtifact!.DocumentCount.Should().Be(1);

            var jsonlLines = await File.ReadAllLinesAsync(options.EmbeddingInputPath);
            jsonlLines.Should().ContainSingle();
            jsonlLines[0].Should().Contain("\"embedding_input\":\"Orders embedding input\"");

            var parsedEmbeddingsArtifact = JsonSerializer.Deserialize<SchemaKnowledgeEmbeddingsArtifact>(
                await File.ReadAllTextAsync(options.EmbeddingsPath),
                JsonSerializerOptions);
            parsedEmbeddingsArtifact.Should().NotBeNull();
            parsedEmbeddingsArtifact!.Documents.Should().ContainSingle();
            parsedEmbeddingsArtifact.Documents.Single().Id.Should().Be("table:orders");
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, recursive: true);
            }
        }
    }
}
