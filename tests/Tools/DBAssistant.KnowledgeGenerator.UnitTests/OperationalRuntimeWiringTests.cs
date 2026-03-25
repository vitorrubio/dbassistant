using FluentAssertions;
using Xunit;

namespace DBAssistant.KnowledgeGenerator.UnitTests;

public sealed class OperationalRuntimeWiringTests
{
    [Fact]
    public void RuntimeArtifacts_ShouldBeIgnoredFromGitAndDockerContexts()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var gitIgnore = File.ReadAllText(Path.Combine(repositoryRoot, ".gitignore"));
        var dockerIgnore = File.ReadAllText(Path.Combine(repositoryRoot, ".dockerignore"));

        gitIgnore.Should().Contain("knowledge/runtime/");
        gitIgnore.Should().Contain("knowledge/*.json");
        gitIgnore.Should().Contain("knowledge/*.jsonl");

        dockerIgnore.Should().Contain("knowledge/runtime");
        dockerIgnore.Should().Contain("knowledge/*.json");
        dockerIgnore.Should().Contain("knowledge/*.jsonl");
    }

    [Fact]
    public void ContainerStartup_ShouldRunKnowledgeGeneratorBeforeApi()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var dockerfile = File.ReadAllText(Path.Combine(repositoryRoot, "Dockerfile"));
        var entrypoint = File.ReadAllText(Path.Combine(repositoryRoot, "docker", "entrypoint.sh"));

        dockerfile.Should().Contain("dotnet publish tools/DBAssistant.KnowledgeGenerator/DBAssistant.KnowledgeGenerator.csproj");
        dockerfile.Should().NotContain("COPY --from=build /src/knowledge ./knowledge");

        entrypoint.Should().Contain("dotnet /app/knowledge-generator/DBAssistant.KnowledgeGenerator.dll");
        entrypoint.Should().Contain("Continuing with INFORMATION_SCHEMA fallback");
        entrypoint.IndexOf("dotnet /app/knowledge-generator/DBAssistant.KnowledgeGenerator.dll", StringComparison.Ordinal)
            .Should().BeLessThan(entrypoint.IndexOf("exec dotnet /app/DBAssistant.Api.dll", StringComparison.Ordinal));
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

        throw new InvalidOperationException("Repository root not found.");
    }
}
