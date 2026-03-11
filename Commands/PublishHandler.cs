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
        var nupkgPath = args.Positional(0);
        var apiKeyArg = args.Option("--api-key", "-k");

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

        // Resolve .nupkg path
        if (nupkgPath is null)
        {
            var nupkgDir = Path.Combine(Directory.GetCurrentDirectory(), "nupkg");
            if (!Directory.Exists(nupkgDir))
            {
                Output.Error("No ./nupkg/ directory found. Run [white]nugetz pack[/] first.");
                return 1;
            }

            var files = Directory.GetFiles(nupkgDir, "*.nupkg")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();

            if (files.Count == 0)
            {
                Output.Error("No .nupkg files found in ./nupkg/. Run [white]nugetz pack[/] first.");
                return 1;
            }

            nupkgPath = files[0];
            Output.Info($"Publishing: [white]{Markup.Escape(Path.GetFileName(nupkgPath))}[/]");
        }
        else if (!File.Exists(nupkgPath))
        {
            Output.Error($"File not found: [white]{Markup.Escape(nupkgPath)}[/]");
            return 1;
        }

        var runner = new DotnetCliRunner();

        var (success, output, error) = await AnsiConsole.Status()
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
                Output.Muted("Bump the version in your .csproj and run [white]nugetz pack[/] again.");
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
