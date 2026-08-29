using System.Text.Json;
using Nugetz.Cli.Infrastructure;
using Nugetz.Cli.Services;
using Nugetz.Cli.UI;
using Spectre.Console;

namespace Nugetz.Cli.Commands;

public static class DoctorHandler
{
    public static async Task<int> RunAsync(string[] rawArgs)
    {
        var args = new Args(rawArgs);
        var format = args.Option("--format") ?? "table";
        if (format is not ("table" or "json"))
        {
            Output.Error("Unsupported format. Use [white]table[/] or [white]json[/].");
            return 2;
        }
        var failOn = args.Option("--fail-on");
        if (failOn is not null && !KnownThresholds.Contains(failOn))
        {
            Output.Error("Unsupported vulnerability threshold. Use low, moderate, high, or critical.");
            return 2;
        }

        var options = new DoctorOptions
        {
            Project = args.Option("--project", "-p"),
            Source = args.Option("--source", "-s"),
            WhyPackage = args.Option("--why"),
            IncludePrerelease = args.Flag("--include-prerelease", "--prerelease"),
            NoRestore = args.Flag("--no-restore"),
        };

        DoctorReport report;
        if (format == "json")
        {
            report = await new DoctorService().InspectAsync(Directory.GetCurrentDirectory(), options);
            Console.WriteLine(JsonSerializer.Serialize(report, NugetzJsonContext.Default.DoctorReport));
        }
        else
        {
            report = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("blue"))
                .StartAsync("Resolving dependency graphs, updates, and advisories...", async _ =>
                    await new DoctorService().InspectAsync(Directory.GetCurrentDirectory(), options));
            Render(report);
        }

        if (report.Status == "unavailable")
            return 2;
        if (args.Flag("--fail-on-outdated") && report.Summary.OutdatedCount > 0)
            return 1;

        if (failOn is not null && HasVulnerabilityAtOrAbove(report, failOn))
            return 1;
        return 0;
    }

    private static void Render(DoctorReport report)
    {
        AnsiConsole.MarkupLine($"\n[bold white]Local package health[/]  [grey]{Markup.Escape(report.Root)}[/]\n");
        foreach (var project in report.Projects)
        {
            var relative = Path.GetRelativePath(report.Root, project.Path);
            if (project.Status == "unavailable")
            {
                Output.Error($"[white]{Markup.Escape(relative)}[/] — {Markup.Escape(project.Error ?? "unavailable")}");
                continue;
            }

            AnsiConsole.MarkupLine($"[blue]●[/] [bold]{Markup.Escape(relative)}[/] [grey]({Markup.Escape(project.Status)})[/]");
            foreach (var framework in project.Frameworks)
            {
                AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(framework.Framework)}[/]");
                foreach (var package in framework.Packages.Where(package => package.IsOutdated || package.Vulnerabilities.Count > 0))
                {
                    var update = package.IsOutdated
                        ? $"[yellow]{Markup.Escape(package.ResolvedVersion)} → {Markup.Escape(package.LatestVersion!)}[/]"
                        : $"[grey]{Markup.Escape(package.ResolvedVersion)}[/]";
                    var vulnerabilities = package.Vulnerabilities.Count > 0
                        ? $" [red]{package.Vulnerabilities.Count} advisory[/]"
                        : "";
                    AnsiConsole.MarkupLine($"    {Markup.Escape(package.Id)}  {update}{vulnerabilities} [grey]{package.Type}[/]");
                }
            }

            foreach (var path in project.DependencyPaths)
            {
                AnsiConsole.MarkupLine($"\n  [grey]Dependency path for {Markup.Escape(path.Package)}:[/]");
                AnsiConsole.WriteLine(path.Output);
            }
        }

        AnsiConsole.MarkupLine(
            $"\n[bold]Summary[/]  {report.Summary.ProjectCount} projects · {report.Summary.PackageCount} package references · " +
            $"[yellow]{report.Summary.OutdatedCount} outdated[/] · [red]{report.Summary.VulnerableCount} vulnerable[/]");
        if (report.Status != "verified")
            Output.Warning("Some checks were unavailable. Unknown results are not counted as healthy.");

        Output.Muted(
            "Continuous repository security, SBOMs, and CI policy: " +
            "https://rorix.io/docs/quickstart?utm_source=nugetz-cli&utm_medium=referral&utm_campaign=doctor");
    }

    private static bool HasVulnerabilityAtOrAbove(DoctorReport report, string threshold)
    {
        var ranks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["any"] = 0,
            ["low"] = 1,
            ["moderate"] = 2,
            ["medium"] = 2,
            ["high"] = 3,
            ["critical"] = 4,
        };
        if (!ranks.TryGetValue(threshold, out var minimum))
            minimum = 3;

        return report.Projects
            .SelectMany(project => project.Frameworks)
            .SelectMany(framework => framework.Packages)
            .SelectMany(package => package.Vulnerabilities)
            .Any(vulnerability => ranks.GetValueOrDefault(vulnerability.Severity, 0) >= minimum);
    }

    private static readonly HashSet<string> KnownThresholds = new(
        ["any", "low", "moderate", "medium", "high", "critical"],
        StringComparer.OrdinalIgnoreCase);
}
