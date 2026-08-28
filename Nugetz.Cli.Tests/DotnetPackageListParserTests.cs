using Nugetz.Cli.Services;

namespace Nugetz.Cli.Tests;

public sealed class DotnetPackageListParserTests
{
    [Fact]
    public void ParsesTopLevelTransitiveUpdatesAndAdvisories()
    {
        const string json = """
        {
          "version": 1,
          "sources": ["https://api.nuget.org/v3/index.json"],
          "projects": [{
            "path": "/repo/App.csproj",
            "frameworks": [{
              "framework": "net10.0",
              "topLevelPackages": [{
                "id": "Direct.Package",
                "requestedVersion": "1.0.0",
                "resolvedVersion": "1.0.0",
                "latestVersion": "2.0.0"
              }],
              "transitivePackages": [{
                "id": "Transitive.Package",
                "resolvedVersion": "3.0.0",
                "vulnerabilities": [{
                  "severity": "high",
                  "advisoryUrl": "https://github.com/advisories/example"
                }]
              }]
            }]
          }]
        }
        """;

        var snapshot = DotnetPackageListParser.Parse(json);

        var framework = Assert.Single(Assert.Single(snapshot.Projects).Frameworks);
        Assert.Equal("net10.0", framework.Framework);
        Assert.Contains(framework.Packages, package =>
            package.Id == "Direct.Package" && package.Type == "top-level" && package.LatestVersion == "2.0.0");
        var transitive = Assert.Single(framework.Packages, package => package.Type == "transitive");
        Assert.Equal("high", Assert.Single(transitive.Vulnerabilities).Severity);
    }
}
