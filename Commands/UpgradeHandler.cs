using System.Text.Json;
using Nugetz.Cli.Infrastructure;
using Nugetz.Cli.Services;
using Nugetz.Cli.UI;
using Spectre.Console;

namespace Nugetz.Cli.Commands;

public static class UpgradeHandler
{
    public static async Task<int> RunAsync(string[] rawArgs)
    {
        var args = new Args(rawArgs);
        var package = args.Positional(0);
        var requestedVersion = args.Option("--to", "--version", "-v");
        if (requestedVersion is not null && package is null)
        {
            Output.Error("[white]--to[/] requires a package name.");
            return 2;
        }

        var format = args.Option("--format") ?? "table";
        if (format is not ("table" or "json"))
        {
            Output.Error("Unsupported format. Use [white]table[/] or [white]json[/].");
            return 2;
        }
        var apply = args.Flag("--apply");
        var doctorOptions = new DoctorOptions
        {
            Project = args.Option("--project", "-p"),
            Source = args.Option("--source", "-s"),
            IncludePrerelease = args.Flag("--include-prerelease", "--prerelease"),
            NoRestore = args.Flag("--no-restore"),
        };

        var report = await new DoctorService().InspectAsync(Directory.GetCurrentDirectory(), doctorOptions);
        var plan = UpgradePlanner.Build(report, package, requestedVersion);

        if (apply && plan.Actions.Count > 0)
        {
            if (!args.Flag("--yes", "-y") && !AnsiConsole.Confirm(
                    $"Apply [yellow]{plan.Actions.Count}[/] package update(s)?"))
            {
                plan.Status = "cancelled";
            }
            else
            {
                plan.Applied = true;
                foreach (var action in plan.Actions)
                {
                    var result = await DotnetCliRunner.RunAsync(
                        ["add", action.Project, "package", action.Package, "--version", action.TargetVersion]);
                    action.Status = result.Success ? "applied" : "failed";
                    action.Error = result.Success ? null :
                        string.IsNullOrWhiteSpace(result.Error) ? result.Output.Trim() : result.Error.Trim();
                }
                plan.Status = plan.Actions.Any(action => action.Status == "failed") ? "partial" : "applied";
            }
        }

        if (format == "json")
            Console.WriteLine(JsonSerializer.Serialize(plan, NugetzJsonContext.Default.UpgradePlan));
        else
            Render(plan, apply);

        if (report.Status == "unavailable")
            return 2;
        return plan.Actions.Any(action => action.Status == "failed") ? 1 : 0;
    }

    private static void Render(UpgradePlan plan, bool apply)
    {
        AnsiConsole.MarkupLine("\n[bold white]Upgrade plan[/]\n");
        if (plan.Actions.Count == 0)
            Output.Success("No matching top-level package updates were found.");

        foreach (var action in plan.Actions)
        {
            var risk = action.MajorUpdate ? " [yellow]major update[/]" : "";
            var central = action.CentralPackageFile is null
                ? ""
                : $" [blue]CPM: {Markup.Escape(Path.GetFileName(action.CentralPackageFile))}[/]";
            var state = action.Status switch
            {
                "applied" => "[green]applied[/]",
                "failed" => "[red]failed[/]",
                _ => "[grey]planned[/]",
            };
            AnsiConsole.MarkupLine(
                $"  [white]{Markup.Escape(action.Package)}[/]  {Markup.Escape(action.CurrentVersion)} → " +
                $"[green]{Markup.Escape(action.TargetVersion)}[/]{risk}{central}  {state}");
            AnsiConsole.MarkupLine($"    [grey]{Markup.Escape(action.Project)}[/]");
            if (action.Error is not null)
                AnsiConsole.MarkupLine($"    [red]{Markup.Escape(action.Error)}[/]");
        }

        foreach (var issue in plan.Issues)
            Output.Warning(Markup.Escape(issue));
        if (!apply && plan.Actions.Count > 0)
            Output.Muted("Preview only. Re-run with --apply to make changes; add --yes for non-interactive CI use.");
    }
}
