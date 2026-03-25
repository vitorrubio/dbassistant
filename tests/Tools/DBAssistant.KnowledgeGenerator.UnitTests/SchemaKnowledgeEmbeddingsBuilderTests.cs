using DBAssistant.KnowledgeGenerator;
using FluentAssertions;
using Xunit;

namespace DBAssistant.KnowledgeGenerator.UnitTests;

public sealed class SchemaKnowledgeEmbeddingsBuilderTests
{
    [Fact]
    public async Task BuildAsync_ShouldCreateEmbeddingsArtifactForAllDocuments()
    {
        var artifact = new SchemaKnowledgeArtifact
        {
            FormatVersion = "schema-rag-v2",
            GeneratedAtUtc = new DateTimeOffset(2026, 3, 25, 0, 0, 0, TimeSpan.Zero),
            Documents =
            [
                new SchemaKnowledgeDocument
                {
                    Id = "table:orders",
                    EmbeddingInput = "orders input"
                },
                new SchemaKnowledgeDocument
                {
                    Id = "relationship:orders.customer_id->customers.id",
                    EmbeddingInput = "orders customer relationship input"
                }
            ]
        };
        var options = new KnowledgeGenerationOptions
        {
            OpenAiEmbeddingModel = "text-embedding-3-small"
        };
        var vectorProvider = new FakeEmbeddingVectorProvider(
        [
            [0.1f, 0.2f],
            [0.3f, 0.4f]
        ]);
        var builder = new SchemaKnowledgeEmbeddingsBuilder(vectorProvider);

        var embeddingsArtifact = await builder.BuildAsync(options, artifact, CancellationToken.None);

        embeddingsArtifact.FormatVersion.Should().Be(artifact.FormatVersion);
        embeddingsArtifact.KnowledgeGeneratedAtUtc.Should().Be(artifact.GeneratedAtUtc);
        embeddingsArtifact.EmbeddingModel.Should().Be("text-embedding-3-small");
        embeddingsArtifact.Documents.Should().HaveCount(2);
        embeddingsArtifact.Documents.Select(document => document.Id)
            .Should()
            .ContainInOrder("table:orders", "relationship:orders.customer_id->customers.id");
        embeddingsArtifact.Documents.First().Embedding.Should().Equal(0.1f, 0.2f);
        embeddingsArtifact.Documents.Last().Embedding.Should().Equal(0.3f, 0.4f);
    }

    private sealed class FakeEmbeddingVectorProvider : IEmbeddingVectorProvider
    {
        private readonly IReadOnlyList<IReadOnlyCollection<float>> _vectors;

        public FakeEmbeddingVectorProvider(IReadOnlyList<IReadOnlyCollection<float>> vectors)
        {
            _vectors = vectors;
        }

        public Task<IReadOnlyList<IReadOnlyCollection<float>>> CreateEmbeddingsAsync(
            KnowledgeGenerationOptions options,
            IReadOnlyList<string> inputs,
            CancellationToken cancellationToken)
        {
            inputs.Should().ContainInOrder("orders input", "orders customer relationship input");
            return Task.FromResult(_vectors);
        }
    }
}
