using System.Text.Json;
using Nugetz.Cli.Infrastructure;
using Nugetz.Cli.Services;
using Nugetz.Cli.UI;
using Spectre.Console;

namespace Nugetz.Cli.Commands;

public static class ValidateHandler
{
    public static async Task<int> RunAsync(string[] rawArgs)
    {
        var args = new Args(rawArgs);
        var input = args.Positional(0) ?? args.Option("--project", "-p");
        var format = args.Option("--format") ?? "table";
        string? temporaryDirectory = null;

        try
        {
            var packagePath = input;
            if (packagePath is null || !packagePath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
            {
                var project = ResolveProject(packagePath);
                if (project is null)
                    return 2;

                temporaryDirectory = Path.Combine(Path.GetTempPath(), $"nugetz-validate-{Guid.NewGuid():N}");
                Directory.CreateDirectory(temporaryDirectory);
                var (success, _, error) = await new DotnetCliRunner().PackAsync(project, temporaryDirectory);
                if (!success)
                {
                    Output.Error("Package build failed.");
                    if (!string.IsNullOrWhiteSpace(error))
                        AnsiConsole.MarkupLine($"[red]{Markup.Escape(error.Trim())}[/]");
                    return 2;
                }
                packagePath = Directory.GetFiles(temporaryDirectory, "*.nupkg")
                    .Where(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }

            if (packagePath is null || !File.Exists(packagePath))
            {
                Output.Error("No .nupkg file was found to validate.");
                return 2;
            }

            var report = PackageValidator.Validate(packagePath);
            if (format == "json")
                Console.WriteLine(JsonSerializer.Serialize(report, NugetzJsonContext.Default.PackageValidationReport));
            else
                Render(report);
            return report.Status == "invalid" ? 1 : 0;
        }
        finally
        {
            if (temporaryDirectory is not null && Directory.Exists(temporaryDirectory))
                Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    public static void Render(PackageValidationReport report)
    {
        var size = report.SizeBytes >= 1_048_576
            ? $"{report.SizeBytes / 1_048_576.0:F1} MB"
            : $"{report.SizeBytes / 1_024.0:F1} KB";
        AnsiConsole.MarkupLine($"\n[bold white]{Markup.Escape(report.PackageId ?? Path.GetFileName(report.PackagePath))}[/] " +
            $"[grey]{Markup.Escape(report.Version ?? "unknown")} · {size} · {report.AssemblyCount} assemblies · " +
            $"{report.DependencyCount} dependencies[/]\n");
        foreach (var issue in report.Issues)
        {
            if (issue.Severity == "error")
                Output.Error($"[white]{Markup.Escape(issue.Code)}[/] — {Markup.Escape(issue.Message)}");
            else
                Output.Warning($"[white]{Markup.Escape(issue.Code)}[/] — {Markup.Escape(issue.Message)}");
        }
        if (report.Issues.Count == 0)
            Output.Success("Package metadata and contents passed validation.");
        AnsiConsole.MarkupLine($"\n[grey]Preview:[/] [cyan]https://nugetz.dev/package/{Markup.Escape(report.PackageId ?? "")}/{Markup.Escape(report.Version ?? "")}[/]");
    }

    private static string? ResolveProject(string? requested)
    {
        if (requested is not null)
        {
            if (File.Exists(requested))
                return requested;
            Output.Error($"Project file not found: [white]{Markup.Escape(requested)}[/]");
            return null;
        }

        var projects = new ProjectDiscoveryService().FindProjects(Directory.GetCurrentDirectory());
        if (projects.Count == 1)
            return projects[0];
        if (projects.Count == 0)
        {
            Output.Error("No .csproj files found.");
            return null;
        }
        Output.Error("Multiple projects found. Pass [white]--project <path>[/].");
        return null;
    }
}
