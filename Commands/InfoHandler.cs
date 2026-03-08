using Nugetz.Cli.Infrastructure;
using Nugetz.Cli.Services;
using Nugetz.Cli.UI;
using Spectre.Console;

namespace Nugetz.Cli.Commands;

public static class InfoHandler
{
    public static async Task<int> RunAsync(string[] rawArgs)
    {
        var args = new Args(rawArgs);
        var package = args.Positional(0);

        if (package is null)
        {
            Output.Error("Missing package name. Usage: [white]nugetz info <package>[/]");
            return 1;
        }

        var api = new NugetApiClient();
        var lookup = new PackageLookupService(api);

        PackageDetailInfo? pkg = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync($"Fetching info for \"{package}\"...", async _ =>
            {
                pkg = await lookup.GetDetailAsync(package);
            });

        if (pkg is null)
        {
            Output.Error($"Package [white]\"{Markup.Escape(package)}\"[/] was not found.");

            var suggestions = await lookup.GetSuggestionsAsync(package);
            if (suggestions.Count > 0)
            {
                AnsiConsole.MarkupLine("\nDid you mean:");
                foreach (var s in suggestions)
                    AnsiConsole.MarkupLine($"  [cyan]- {Markup.Escape(s)}[/]");
            }

            return 1;
        }

        AnsiConsole.WriteLine();
        Tables.RenderPackageDetail(pkg);

        return 0;
    }
}
