using Nugetz.Cli.Infrastructure;
using Nugetz.Cli.Services;
using Nugetz.Cli.UI;
using Spectre.Console;

namespace Nugetz.Cli.Commands;

public static class PackHandler
{
    public static async Task<int> RunAsync(string[] rawArgs)
    {
        var args = new Args(rawArgs);
        var projectPath = args.Positional(0);

        // If no explicit project, discover
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
                        .Title("Select a project to pack:")
                        .PageSize(15)
                        .AddChoices(projects));
            }
        }

        var runner = new DotnetCliRunner();

        Output.Info($"Packing [green]{Markup.Escape(projectPath)}[/]...");
        AnsiConsole.WriteLine();

        var (success, output, error) = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("Running dotnet pack...", async _ =>
                await runner.PackAsync(projectPath));

        if (!success)
        {
            Output.Error("Pack failed.");
            if (!string.IsNullOrWhiteSpace(error))
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(error.Trim())}[/]");
            return 1;
        }

        // Find the generated .nupkg file
        var nupkgDir = Path.Combine(Directory.GetCurrentDirectory(), "nupkg");
        if (Directory.Exists(nupkgDir))
        {
            var nupkgFiles = Directory.GetFiles(nupkgDir, "*.nupkg")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToList();

            if (nupkgFiles.Count > 0)
            {
                var latest = nupkgFiles[0];
                var fileInfo = new FileInfo(latest);
                var size = FormatFileSize(fileInfo.Length);

                AnsiConsole.WriteLine();
                Output.Success($"Package created: [white]{Markup.Escape(Path.GetFileName(latest))}[/] ({size})");
                Output.Muted($"  Path: {Markup.Escape(latest)}");
                AnsiConsole.MarkupLine($"\n[grey]Publish with:[/] [white]nugetz publish[/]");
            }
        }
        else
        {
            Output.Success("Pack completed.");
        }

        return 0;
    }

    private static string FormatFileSize(long bytes) => bytes switch
    {
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024 => $"{bytes / 1_024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
