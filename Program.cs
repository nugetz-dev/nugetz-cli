using Nugetz.Cli.Commands;
using Nugetz.Cli.UI;

if (args.Length == 0)
{
    ShowHelp();
    return 0;
}

var command = args[0].ToLowerInvariant();

return command switch
{
    "install" => await InstallHandler.RunAsync(args[1..]),
    "search" => await SearchHandler.RunAsync(args[1..]),
    "info" => await InfoHandler.RunAsync(args[1..]),
    "publish" => await PublishHandler.RunAsync(args[1..]),
    "apikey" => HandleApiKey(args[1..]),
    "--help" or "-h" or "help" => ShowHelp(),
    "--version" => ShowVersion(),
    _ => ShowUnknown(command),
};

static int HandleApiKey(string[] subArgs)
{
    if (subArgs.Length == 0)
        return ApiKeyHandler.Status();

    var sub = subArgs[0].ToLowerInvariant();
    return sub switch
    {
        "set" => ApiKeyHandler.Set(subArgs[1..]),
        "remove" or "rm" or "delete" => ApiKeyHandler.Remove(),
        "status" => ApiKeyHandler.Status(),
        _ => ApiKeyHandler.Status(),
    };
}

static int ShowHelp()
{
    Output.Info("[bold white]nugetz[/] — A modern CLI for NuGet packages\n");
    Spectre.Console.AnsiConsole.MarkupLine("[grey]USAGE:[/]");
    Spectre.Console.AnsiConsole.MarkupLine("  nugetz [green]<command>[/] [grey][[options]][/]\n");
    Spectre.Console.AnsiConsole.MarkupLine("[grey]COMMANDS:[/]");
    Spectre.Console.AnsiConsole.MarkupLine("  [green]install[/]       <package>   Install a NuGet package into one or more projects");
    Spectre.Console.AnsiConsole.MarkupLine("  [green]search[/]        <query>    Search for NuGet packages");
    Spectre.Console.AnsiConsole.MarkupLine("  [green]info[/]          <package>   Show detailed package information");
    Spectre.Console.AnsiConsole.MarkupLine("  [green]publish[/]       [[project]]  Pack and publish to nuget.org");
    Spectre.Console.AnsiConsole.MarkupLine("  [green]apikey[/]        <sub>       Manage NuGet API key (set|remove|status)\n");
    Spectre.Console.AnsiConsole.MarkupLine("[grey]OPTIONS:[/]");
    Spectre.Console.AnsiConsole.MarkupLine("  [grey]--help, -h[/]          Show help");
    Spectre.Console.AnsiConsole.MarkupLine("  [grey]--version[/]           Show version");
    return 0;
}

static int ShowVersion()
{
    Spectre.Console.AnsiConsole.MarkupLine("[grey]nugetz[/] 0.3.0");
    return 0;
}

static int ShowUnknown(string cmd)
{
    Output.Error($"Unknown command: [white]{Spectre.Console.Markup.Escape(cmd)}[/]");
    Output.Muted("Run [white]nugetz --help[/] for usage.");
    return 1;
}
