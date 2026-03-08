using Nugetz.Cli.Infrastructure;

namespace Nugetz.Cli.Services;

public sealed record InstallResult(string ProjectPath, bool Success, string? Error);

public sealed class PackageInstallService(DotnetCliRunner runner)
{
    public async Task<List<InstallResult>> InstallAsync(
        string packageName, string? version, List<string> projectPaths)
    {
        var results = new List<InstallResult>();

        foreach (var project in projectPaths)
        {
            var (success, _, error) = await runner.InstallPackageAsync(project, packageName, version);
            results.Add(new InstallResult(project, success, success ? null : error.Trim()));
        }

        return results;
    }
}
