using System.Text.Json;
using DBAssistant.KnowledgeGenerator;
using FluentAssertions;
using Xunit;

namespace DBAssistant.KnowledgeGenerator.UnitTests;

public sealed class SchemaKnowledgeArtifactWriterTests
{
    [Fact]
    public async Task WriteAsync_ShouldRespectConfiguredOutputPaths()
    {
        var temporaryRoot = Path.Combine(Path.GetTempPath(), $"dbassistant-knowledge-tests-{Guid.NewGuid():N}");
        var outputDirectory = Path.Combine(temporaryRoot, "custom-output");
        var options = new KnowledgeGenerationOptions
        {
            OutputDirectory = outputDirectory,
            SchemaDocumentsPath = Path.Combine(outputDirectory, "schema-documents.json"),
            EmbeddingInputPath = Path.Combine(outputDirectory, "schema-embedding-input.jsonl")
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

        try
        {
            var writer = new SchemaKnowledgeArtifactWriter();
            var result = await writer.WriteAsync(options, artifact, embeddingRecords, CancellationToken.None);

            result.SchemaDocumentsPath.Should().Be(options.SchemaDocumentsPath);
            result.EmbeddingInputPath.Should().Be(options.EmbeddingInputPath);
            File.Exists(options.SchemaDocumentsPath).Should().BeTrue();
            File.Exists(options.EmbeddingInputPath).Should().BeTrue();

            var parsedArtifact = JsonSerializer.Deserialize<SchemaKnowledgeArtifact>(await File.ReadAllTextAsync(options.SchemaDocumentsPath));
            parsedArtifact.Should().NotBeNull();
            parsedArtifact!.DocumentCount.Should().Be(1);

            var jsonlLines = await File.ReadAllLinesAsync(options.EmbeddingInputPath);
            jsonlLines.Should().ContainSingle();
            jsonlLines[0].Should().Contain("\"embedding_input\":\"Orders embedding input\"");
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
