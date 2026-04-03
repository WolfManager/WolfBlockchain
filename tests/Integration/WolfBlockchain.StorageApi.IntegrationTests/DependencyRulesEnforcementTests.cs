using System.Xml.Linq;

namespace WolfBlockchain.StorageApi.IntegrationTests;

public class DependencyRulesEnforcementTests
{
    [Fact]
    public void SrcProjects_ShouldRespectAllowedModuleDependencies()
    {
        var root = FindRepositoryRoot();

        var allowed = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["WolfBlockchain.Protocol"] = new(StringComparer.OrdinalIgnoreCase),
            ["WolfBlockchain.Observability"] = new(StringComparer.OrdinalIgnoreCase),
            ["WolfBlockchain.Core"] = new(StringComparer.OrdinalIgnoreCase) { "WolfBlockchain.Protocol", "WolfBlockchain.Observability" },
            ["WolfBlockchain.Storage"] = new(StringComparer.OrdinalIgnoreCase) { "WolfBlockchain.Protocol", "WolfBlockchain.Core", "WolfBlockchain.Observability" },
            ["WolfBlockchain.Consensus"] = new(StringComparer.OrdinalIgnoreCase) { "WolfBlockchain.Protocol", "WolfBlockchain.Core", "WolfBlockchain.Observability" },
            ["WolfBlockchain.Networking"] = new(StringComparer.OrdinalIgnoreCase) { "WolfBlockchain.Protocol", "WolfBlockchain.Core", "WolfBlockchain.Observability" },
            ["WolfBlockchain.Wallet"] = new(StringComparer.OrdinalIgnoreCase) { "WolfBlockchain.Protocol", "WolfBlockchain.Core", "WolfBlockchain.Observability" },
            ["WolfBlockchain.Agents"] = new(StringComparer.OrdinalIgnoreCase) { "WolfBlockchain.Protocol", "WolfBlockchain.Core", "WolfBlockchain.Wallet", "WolfBlockchain.Observability" },
            ["WolfBlockchain.Api"] = new(StringComparer.OrdinalIgnoreCase) { "WolfBlockchain.Protocol", "WolfBlockchain.Core", "WolfBlockchain.Wallet", "WolfBlockchain.Agents", "WolfBlockchain.Observability", "WolfBlockchain.Storage", "WolfBlockchain.Consensus" }
        };

        foreach (var kvp in allowed)
        {
            var projectName = kvp.Key;
            var csprojPath = Path.Combine(root, "src", projectName, $"{projectName}.csproj");
            Assert.True(File.Exists(csprojPath), $"Missing project file: {csprojPath}");

            var references = GetProjectReferences(csprojPath);
            var disallowed = references.Where(reference => !kvp.Value.Contains(reference)).ToArray();

            Assert.True(disallowed.Length == 0,
                $"Project '{projectName}' has disallowed references: {string.Join(", ", disallowed)}");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var slnPath = Path.Combine(directory.FullName, "WolfBlockchain.sln");
            if (File.Exists(slnPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test runtime directory.");
    }

    private static IReadOnlyList<string> GetProjectReferences(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);

        return doc
            .Descendants()
            .Where(x => x.Name.LocalName == "ProjectReference")
            .Select(x => x.Attribute("Include")?.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(GetProjectNameFromReference)
            .ToArray();
    }

    private static string GetProjectNameFromReference(string? includePath)
    {
        var normalized = includePath!.Replace('\\', '/');
        return Path.GetFileNameWithoutExtension(normalized);
    }
}
