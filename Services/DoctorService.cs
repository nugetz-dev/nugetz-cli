using System.Text.Json;
using Nugetz.Cli.Infrastructure;

namespace Nugetz.Cli.Services;

public sealed class DoctorOptions
{
    public string? Project { get; init; }
    public string? Source { get; init; }
    public string? WhyPackage { get; init; }
    public bool IncludePrerelease { get; init; }
    public bool NoRestore { get; init; }
}

public sealed class DoctorService
{
    public async Task<DoctorReport> InspectAsync(string root, DoctorOptions options)
    {
        var fullRoot = Path.GetFullPath(root);
        var report = new DoctorReport
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Root = fullRoot,
            Status = "verified",
        };

        var projects = ResolveProjects(fullRoot, options.Project);
        if (projects.Count == 0)
        {
            report.Status = "unavailable";
            report.Issues.Add("No .csproj files were found.");
            return report;
        }

        foreach (var projectPath in projects)
            report.Projects.Add(await InspectProjectAsync(projectPath, options, report.Sources));

        PopulateSummary(report);
        return report;
    }

    private static List<string> ResolveProjects(string root, string? requestedProject)
    {
        if (requestedProject is not null)
        {
            var path = Path.GetFullPath(requestedProject, root);
            return File.Exists(path) ? [path] : [];
        }

        return new ProjectDiscoveryService()
            .FindProjects(root)
            .Select(path => Path.GetFullPath(path, root))
            .ToList();
    }

    private static async Task<DoctorProject> InspectProjectAsync(
        string projectPath,
        DoctorOptions options,
        List<string> reportSources)
    {
        var basicResult = await DotnetCliRunner.RunAsync(BuildListArguments(projectPath, options, filter: null));
        if (!basicResult.Success)
        {
            return new DoctorProject
            {
                Path = projectPath,
                Status = "unavailable",
                Error = ErrorMessage(basicResult),
            };
        }

        PackageListSnapshot basic;
        try
        {
            basic = DotnetPackageListParser.Parse(basicResult.Output);
        }
        catch (JsonException exception)
        {
            return new DoctorProject
            {
                Path = projectPath,
                Status = "unavailable",
                Error = $"The .NET SDK returned invalid package JSON: {exception.Message}",
            };
        }

        var project = basic.Projects.FirstOrDefault() ?? new DoctorProject
        {
            Path = projectPath,
            Status = "verified",
        };
        AddSources(reportSources, basic.Sources);

        var outdatedTask = DotnetCliRunner.RunAsync(BuildListArguments(projectPath, options, "outdated"));
        var vulnerableTask = DotnetCliRunner.RunAsync(BuildListArguments(projectPath, options, "vulnerable"));
        await Task.WhenAll(outdatedTask, vulnerableTask);

        var outdatedResult = await outdatedTask;
        var vulnerableResult = await vulnerableTask;
        var partial = false;

        if (outdatedResult.Success)
        {
            try
            {
                var outdated = DotnetPackageListParser.Parse(outdatedResult.Output);
                Merge(project, outdated, mergeUpdates: true, mergeVulnerabilities: false);
                AddSources(reportSources, outdated.Sources);
            }
            catch (JsonException)
            {
                partial = true;
                foreach (var package in Packages(project))
                    package.UpdateStatus = "unknown";
            }
        }
        else
        {
            partial = true;
            foreach (var package in Packages(project))
                package.UpdateStatus = "unknown";
        }

        if (vulnerableResult.Success)
        {
            try
            {
                var vulnerable = DotnetPackageListParser.Parse(vulnerableResult.Output);
                Merge(project, vulnerable, mergeUpdates: false, mergeVulnerabilities: true);
                AddSources(reportSources, vulnerable.Sources);
            }
            catch (JsonException)
            {
                partial = true;
                foreach (var package in Packages(project))
                    package.VulnerabilityStatus = "unavailable";
            }
        }
        else
        {
            partial = true;
            foreach (var package in Packages(project))
                package.VulnerabilityStatus = "unavailable";
        }

        foreach (var package in Packages(project))
            if (package.UpdateStatus != "unknown")
                package.UpdateStatus = package.IsOutdated ? "outdated" : "current";

        if (options.WhyPackage is not null)
        {
            var why = await DotnetCliRunner.RunAsync(["nuget", "why", projectPath, options.WhyPackage]);
            project.DependencyPaths.Add(new DependencyPathResult
            {
                Package = options.WhyPackage,
                Status = why.Success ? "verified" : "unavailable",
                Output = why.Success ? why.Output.Trim() : ErrorMessage(why),
            });
            partial |= !why.Success;
        }

        project.Status = partial ? "partial" : "verified";
        return project;
    }

    private static List<string> BuildListArguments(
        string projectPath,
        DoctorOptions options,
        string? filter)
    {
        var arguments = new List<string>
        {
            "package", "list", "--project", projectPath, "--include-transitive", "--format", "json",
        };
        if (filter is not null)
            arguments.Add($"--{filter}");
        if (filter == "outdated" && options.IncludePrerelease)
            arguments.Add("--include-prerelease");
        if (options.NoRestore)
            arguments.Add("--no-restore");
        if (options.Source is not null && filter is not null)
            arguments.AddRange(["--source", options.Source]);
        return arguments;
    }

    private static void Merge(
        DoctorProject target,
        PackageListSnapshot snapshot,
        bool mergeUpdates,
        bool mergeVulnerabilities)
    {
        var sourceProject = snapshot.Projects.FirstOrDefault();
        if (sourceProject is null)
            return;

        foreach (var sourceFramework in sourceProject.Frameworks)
        {
            var targetFramework = target.Frameworks.FirstOrDefault(
                framework => framework.Framework.Equals(sourceFramework.Framework, StringComparison.OrdinalIgnoreCase));
            if (targetFramework is null)
                continue;

            foreach (var sourcePackage in sourceFramework.Packages)
            {
                var targetPackage = targetFramework.Packages.FirstOrDefault(package =>
                    package.Id.Equals(sourcePackage.Id, StringComparison.OrdinalIgnoreCase) &&
                    package.Type == sourcePackage.Type);
                if (targetPackage is null)
                    continue;

                if (mergeUpdates)
                    targetPackage.LatestVersion = sourcePackage.LatestVersion;
                if (mergeVulnerabilities)
                    targetPackage.Vulnerabilities.AddRange(sourcePackage.Vulnerabilities);
            }
        }
    }

    private static IEnumerable<DoctorPackage> Packages(DoctorProject project) =>
        project.Frameworks.SelectMany(framework => framework.Packages);

    private static void AddSources(List<string> target, IEnumerable<string> sources)
    {
        foreach (var source in sources)
            if (!target.Contains(source, StringComparer.OrdinalIgnoreCase))
                target.Add(source);
    }

    private static void PopulateSummary(DoctorReport report)
    {
        report.Summary.ProjectCount = report.Projects.Count;
        report.Summary.UnknownProjectCount = report.Projects.Count(project => project.Status != "verified");
        report.Summary.FrameworkCount = report.Projects.Sum(project => project.Frameworks.Count);
        var packages = report.Projects.SelectMany(project => project.Frameworks).SelectMany(framework => framework.Packages).ToList();
        report.Summary.PackageCount = packages.Count;
        report.Summary.OutdatedCount = packages.Count(package => package.IsOutdated);
        report.Summary.VulnerableCount = packages.Count(package => package.Vulnerabilities.Count > 0);
        report.Status = report.Projects.All(project => project.Status == "unavailable")
            ? "unavailable"
            : report.Projects.Any(project => project.Status != "verified") ? "partial" : "verified";
    }

    private static string ErrorMessage(CommandResult result) =>
        string.IsNullOrWhiteSpace(result.Error)
            ? string.IsNullOrWhiteSpace(result.Output) ? $"dotnet exited with code {result.ExitCode}." : result.Output.Trim()
            : result.Error.Trim();
}
