using Nugetz.Cli.Infrastructure;
using Nugetz.Cli.Services;
using Nugetz.Cli.UI;
using Spectre.Console;

namespace Nugetz.Cli.Commands;

public static class SearchHandler
{
    public static async Task<int> RunAsync(string[] rawArgs)
    {
        var args = new Args(rawArgs);
        var query = args.Positional(0);

        if (query is null)
        {
            Output.Error("Missing search query. Usage: [white]nugetz search <query>[/]");
            return 1;
        }

        var limit = args.OptionInt(10, "--limit", "-l");
        var prerelease = args.Flag("--prerelease");

        var api = new NugetApiClient();
        var lookup = new PackageLookupService(api);

        SearchResponse? response = null;

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync($"Searching for \"{query}\"...", async _ =>
            {
                response = await lookup.SearchAsync(query, limit, prerelease);
            });

        if (response is null || response.Data.Count == 0)
        {
            Output.Warning($"No packages found for [white]\"{Markup.Escape(query)}\"[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"\nResults for [green]\"{Markup.Escape(query)}\"[/] ({response.TotalHits} total)\n");
        Tables.RenderSearchResults(response.Data);

        return 0;
    }
}
