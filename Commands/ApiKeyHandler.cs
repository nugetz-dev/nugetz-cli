using Nugetz.Cli.Services;
using Nugetz.Cli.UI;
using Spectre.Console;

namespace Nugetz.Cli.Commands;

public static class ApiKeyHandler
{
    public static int Set(string[] rawArgs)
    {
        var args = new Args(rawArgs);
        var key = args.Positional(0);

        if (string.IsNullOrWhiteSpace(key))
        {
            Output.Error("Missing API key. Usage: [white]nugetz apikey set <key>[/]");
            AnsiConsole.MarkupLine("\n[grey]Get your key at:[/] [cyan]https://www.nuget.org/account/apikeys[/]");
            return 1;
        }

        ConfigService.SetApiKey(key);

        var masked = key.Length > 4
            ? new string('*', key.Length - 4) + key[^4..]
            : "****";

        Output.Success($"API key saved: [grey]{Markup.Escape(masked)}[/]");
        return 0;
    }

    public static int Remove()
    {
        ConfigService.RemoveApiKey();
        Output.Success("API key removed.");
        return 0;
    }

    public static int Status()
    {
        var key = ConfigService.GetApiKey();
        if (key is null)
        {
            Output.Warning("No API key configured.");
            AnsiConsole.MarkupLine("[grey]Set one with:[/] [white]nugetz apikey set <key>[/]");
        }
        else
        {
            var masked = key.Length > 4
                ? new string('*', key.Length - 4) + key[^4..]
                : "****";
            Output.Success($"API key is configured: [grey]{Markup.Escape(masked)}[/]");
        }
        return 0;
    }
}
