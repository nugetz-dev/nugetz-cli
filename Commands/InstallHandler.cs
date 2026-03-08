using Nugetz.Cli.Infrastructure;
using Nugetz.Cli.Services;
using Nugetz.Cli.UI;
using Spectre.Console;

namespace Nugetz.Cli.Commands;

public static class InstallHandler
{
    public static async Task<int> RunAsync(string[] rawArgs)
    {
        var args = new Args(rawArgs);
        var packageArg = args.Positional(0);

        if (packageArg is null)
        {
            Output.Error("Missing package name. Usage: [white]nugetz install <package>[/]");
            return 1;
        }

        // Parse package@version syntax
        var packageName = packageArg;
        var version = args.Option("--version", "-v");

        if (version is null && packageName.Contains('@'))
        {
            var parts = packageName.Split('@', 2);
            packageName = parts[0];
            version = parts[1];
        }

        var all = args.Flag("--all");
        var project = args.Option("--project", "-p");
        var yes = args.Flag("--yes", "-y");

        var api = new NugetApiClient();
        var lookup = new PackageLookupService(api);
        var runner = new DotnetCliRunner();
        var installer = new PackageInstallService(runner);
        var discovery = new ProjectDiscoveryService();

        // Resolve package
        PackageInfo? pkg = null;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("Resolving package...", async _ =>
            {
                pkg = await lookup.ResolvePackageAsync(packageName);
            });

        if (pkg is null)
        {
            Output.Error($"Package [white]\"{Markup.Escape(packageName)}\"[/] was not found.");

            var suggestions = await lookup.GetSuggestionsAsync(packageName);
            if (suggestions.Count > 0)
            {
                AnsiConsole.MarkupLine("\nDid you mean:");
                foreach (var s in suggestions)
                    AnsiConsole.MarkupLine($"  [cyan]- {Markup.Escape(s)}[/]");
            }

            return 1;
        }

        packageName = pkg.Id;
        version ??= pkg.Version;

        // Find projects
        List<string> projects = [];

        if (project is not null)
        {
            if (!File.Exists(project))
            {
                Output.Error($"Project file not found: [white]{Markup.Escape(project)}[/]");
                return 1;
            }
            projects = [project];
        }
        else
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("blue"))
                .Start("Scanning for projects...", _ =>
                {
                    projects = discovery.FindProjects(Directory.GetCurrentDirectory());
                });

            if (projects.Count == 0)
            {
                Output.Error("No .csproj files found in this directory.");
                Output.Muted("Run this command inside a .NET project or repository.");
                return 1;
            }
        }

        // Determine target projects
        List<string> targets;

        if (projects.Count == 1)
        {
            Output.Info($"Found project: [white]{Markup.Escape(projects[0])}[/]");
            targets = projects;
        }
        else if (all)
        {
            Output.Info($"Installing into all [blue]{projects.Count}[/] projects");
            targets = projects;
        }
        else
        {
            targets = ProjectSelectionPrompt.Show(projects, packageName);

            if (targets.Count == 0)
            {
                Output.Warning("No projects selected. Installation cancelled.");
                return 0;
            }
        }

        // Confirm
        if (!yes && targets.Count > 1)
        {
            var confirm = AnsiConsole.Confirm(
                $"Install [green]{Markup.Escape(packageName)} {Markup.Escape(version)}[/] into [blue]{targets.Count}[/] projects?");

            if (!confirm)
            {
                Output.Muted("Installation cancelled.");
                return 0;
            }
        }

        // Install
        AnsiConsole.WriteLine();
        var results = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync($"Installing {packageName} {version}...", async _ =>
                await installer.InstallAsync(packageName, version, targets));

        // Summary
        AnsiConsole.WriteLine();
        foreach (var result in results)
        {
            if (result.Success)
                Output.Success($"Installed [green]{Markup.Escape(packageName)} {Markup.Escape(version)}[/] into [white]{Markup.Escape(result.ProjectPath)}[/]");
            else
                Output.Error($"Failed in [white]{Markup.Escape(result.ProjectPath)}[/]: {Markup.Escape(result.Error ?? "unknown error")}");
        }

        var failed = results.Count(r => !r.Success);
        return failed > 0 ? 1 : 0;
    }
}
