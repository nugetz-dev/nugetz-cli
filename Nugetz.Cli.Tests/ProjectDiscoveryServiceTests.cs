using Nugetz.Cli.Services;

namespace Nugetz.Cli.Tests;

public sealed class ProjectDiscoveryServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        $"nugetz-tests-{Guid.NewGuid():N}");

    [Fact]
    public void DiscoversProjectsInCustomerFriendlyPriorityOrder()
    {
        WriteProject("misc/Misc.csproj");
        WriteProject("tests/App.Tests.csproj");
        WriteProject("services/Worker.csproj");
        WriteProject("apps/Web.csproj");
        WriteProject("src/Core.csproj");

        var projects = new ProjectDiscoveryService().FindProjects(_root);

        Assert.Equal(
            [
                "src/Core.csproj",
                "apps/Web.csproj",
                "services/Worker.csproj",
                "tests/App.Tests.csproj",
                "misc/Misc.csproj",
            ],
            projects.Select(path => path.Replace('\\', '/')));
    }

    private void WriteProject(string relativePath)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "<Project />");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
