using Spectre.Console;

namespace Nugetz.Cli.UI;

public static class ProjectSelectionPrompt
{
    public static List<string> Show(List<string> projects, string packageName)
    {
        AnsiConsole.MarkupLine($"\nFound [blue]{projects.Count}[/] projects\n");

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title($"Select projects to install [green]{packageName}[/] into:")
                .NotRequired()
                .PageSize(15)
                .MoreChoicesText("[grey](Move up and down to see more projects)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
                .AddChoices(projects));

        return selected;
    }
}
