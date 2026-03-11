using Nugetz.Cli.Infrastructure;
using Nugetz.Cli.Services;
using Nugetz.Cli.UI;
using Spectre.Console;

namespace Nugetz.Cli.Commands;

public static class PublishHandler
{
    public static async Task<int> RunAsync(string[] rawArgs)
    {
        var args = new Args(rawArgs);
        var apiKeyArg = args.Option("--api-key", "-k");
        var project = args.Option("--project", "-p");
        var nupkgPath = args.Positional(0);

        // Resolve API key
        var apiKey = apiKeyArg ?? ConfigService.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Output.Error("No API key found.");
            AnsiConsole.MarkupLine("[grey]Set one with:[/] [white]nugetz apikey set <key>[/]");
            AnsiConsole.MarkupLine("[grey]Or pass:[/] [white]nugetz publish --api-key <key>[/]");
            AnsiConsole.MarkupLine("[grey]Or set:[/] [white]NUGET_API_KEY[/] environment variable");
            return 1;
        }

        var runner = new DotnetCliRunner();

        // If a .nupkg path was given, skip packing and push directly
        if (nupkgPath is not null && nupkgPath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(nupkgPath))
            {
                Output.Error($"File not found: [white]{Markup.Escape(nupkgPath)}[/]");
                return 1;
            }
            return await PushAsync(runner, nupkgPath, apiKey);
        }

        // --- Pack phase ---
        var projectPath = project ?? nupkgPath;

        if (projectPath is null)
        {
            var discovery = new ProjectDiscoveryService();
            var projects = discovery.FindProjects(Directory.GetCurrentDirectory());

            if (projects.Count == 0)
            {
                Output.Error("No .csproj files found in this directory.");
                Output.Muted("Run this command inside a .NET project or repository.");
                return 1;
            }

            if (projects.Count == 1)
            {
                projectPath = projects[0];
                Output.Info($"Found project: [white]{Markup.Escape(projectPath)}[/]");
            }
            else
            {
                projectPath = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a project to publish:")
                        .PageSize(15)
                        .AddChoices(projects));
            }
        }
        else if (!File.Exists(projectPath))
        {
            Output.Error($"Project file not found: [white]{Markup.Escape(projectPath)}[/]");
            return 1;
        }

        Output.Info($"Packing [green]{Markup.Escape(projectPath)}[/]...");

        var (packSuccess, _, packError) = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("Running dotnet pack...", async _ =>
                await runner.PackAsync(projectPath));

        if (!packSuccess)
        {
            Output.Error("Pack failed.");
            if (!string.IsNullOrWhiteSpace(packError))
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(packError.Trim())}[/]");
            return 1;
        }

        // Find the generated .nupkg
        var nupkgDir = Path.Combine(Directory.GetCurrentDirectory(), "nupkg");
        if (!Directory.Exists(nupkgDir))
        {
            Output.Error("Pack succeeded but no ./nupkg/ directory found.");
            return 1;
        }

        var latest = Directory.GetFiles(nupkgDir, "*.nupkg")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (latest is null)
        {
            Output.Error("Pack succeeded but no .nupkg file found in ./nupkg/.");
            return 1;
        }

        var fileInfo = new FileInfo(latest);
        var size = fileInfo.Length switch
        {
            >= 1_048_576 => $"{fileInfo.Length / 1_048_576.0:F1} MB",
            >= 1_024 => $"{fileInfo.Length / 1_024.0:F1} KB",
            _ => $"{fileInfo.Length} B"
        };

        Output.Success($"Packed [white]{Markup.Escape(Path.GetFileName(latest))}[/] ({size})");
        AnsiConsole.WriteLine();

        // --- Push phase ---
        return await PushAsync(runner, latest, apiKey);
    }

    private static async Task<int> PushAsync(DotnetCliRunner runner, string nupkgPath, string apiKey)
    {
        Output.Info($"Publishing [white]{Markup.Escape(Path.GetFileName(nupkgPath))}[/]...");

        var (success, _, error) = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("Pushing to nuget.org...", async _ =>
                await runner.PushAsync(nupkgPath, apiKey));

        if (!success)
        {
            if (error.Contains("401") || error.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
            {
                Output.Error("Authentication failed — invalid API key.");
                AnsiConsole.MarkupLine("[grey]Update your key with:[/] [white]nugetz apikey set <key>[/]");
            }
            else if (error.Contains("409") || error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                Output.Error("This package version already exists on nuget.org.");
                Output.Muted("Bump the version in your .csproj and try again.");
            }
            else
            {
                Output.Error("Push failed.");
                if (!string.IsNullOrWhiteSpace(error))
                    AnsiConsole.MarkupLine($"[red]{Markup.Escape(error.Trim())}[/]");
            }
            return 1;
        }

        AnsiConsole.WriteLine();
        Output.Success($"Published [green]{Markup.Escape(Path.GetFileName(nupkgPath))}[/] to nuget.org");
        return 0;
    }
}
