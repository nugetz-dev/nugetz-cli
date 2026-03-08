using Nugetz.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Nugetz.Cli.UI;

public static class Tables
{
    public static void RenderSearchResults(List<PackageInfo> packages)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[blue]Package[/]").NoWrap())
            .AddColumn(new TableColumn("[blue]Version[/]"))
            .AddColumn(new TableColumn("[blue]Downloads[/]").RightAligned())
            .AddColumn(new TableColumn("[blue]Description[/]"));

        foreach (var pkg in packages)
        {
            var name = pkg.Verified
                ? $"[green]{Markup.Escape(pkg.Id)}[/] [blue]✓[/]"
                : $"[white]{Markup.Escape(pkg.Id)}[/]";

            table.AddRow(
                name,
                $"[grey]{Markup.Escape(pkg.Version)}[/]",
                $"[yellow]{Output.FormatDownloads(pkg.TotalDownloads)}[/]",
                Markup.Escape(Truncate(pkg.Description, 60)));
        }

        AnsiConsole.Write(table);
    }

    public static void RenderPackageDetail(PackageDetailInfo pkg)
    {
        var rows = new List<IRenderable>
        {
            new Markup($"[bold white]{Markup.Escape(pkg.Id)}[/]" +
                (pkg.Verified ? " [blue]✓ verified[/]" : "")),
            new Markup(""),
            new Markup($"[grey]Version:[/]      [white]{Markup.Escape(pkg.Version)}[/]"),
            new Markup($"[grey]Downloads:[/]    [yellow]{Output.FormatDownloads(pkg.TotalDownloads)}[/]"),
            new Markup($"[grey]Published:[/]    [white]{Output.TimeAgo(pkg.Published)}[/]"),
            new Markup($"[grey]Authors:[/]      [white]{Markup.Escape(pkg.Authors ?? "—")}[/]"),
            new Markup($"[grey]License:[/]      [white]{Markup.Escape(pkg.LicenseExpression ?? "—")}[/]"),
            new Markup($"[grey]Project:[/]      [cyan]{Markup.Escape(pkg.ProjectUrl ?? "—")}[/]"),
            new Markup($"[grey]Dependencies:[/] [white]{pkg.DependencyCount}[/]"),
            new Markup($"[grey]Vulns:[/]        {(pkg.VulnerabilityCount == 0 ? "[green]✓ 0[/]" : $"[red]{pkg.VulnerabilityCount}[/]")}"),
        };

        if (pkg.TargetFrameworks.Count > 0)
        {
            var fws = string.Join(" ", pkg.TargetFrameworks.Select(fw => $"[blue]{Markup.Escape(fw)}[/]"));
            rows.Add(new Markup($"[grey]Frameworks:[/]   {fws}"));
        }

        // Health Score
        if (pkg.HealthScore is not null)
        {
            var h = pkg.HealthScore;
            var gradeColor = h.Grade switch
            {
                "A" => "green",
                "B" => "blue",
                "C" => "yellow",
                "D" => "orange3",
                _ => "red"
            };
            rows.Add(new Markup(""));
            rows.Add(new Markup($"[grey]Health:[/]       [{gradeColor}]{h.Grade}[/] [{gradeColor}]{h.Score}/100[/]"));
            rows.Add(new Markup($"  [grey]Freshness {h.Freshness}/25 · Popularity {h.Popularity}/25 · Maintenance {h.Maintenance}/25 · Security {h.Security}/25[/]"));
        }

        // Community Signals
        if (pkg.CommunitySignals is not null)
        {
            var c = pkg.CommunitySignals;
            rows.Add(new Markup(""));
            rows.Add(new Markup(
                $"[grey]GitHub:[/]       [yellow]★ {Output.FormatDownloads(c.Stars)}[/]  " +
                $"[grey]Issues:[/] [white]{Output.FormatDownloads(c.OpenIssues)}[/]  " +
                $"[grey]Contributors:[/] [white]{Output.FormatDownloads(c.Contributors)}[/]  " +
                $"[grey]Last commit:[/] [white]{Output.TimeAgo(c.LastCommit)}[/]"));
        }

        rows.Add(new Markup(""));
        rows.Add(new Markup($"[grey]{Markup.Escape(pkg.Description)}[/]"));

        if (pkg.Tags.Count > 0)
            rows.Add(new Markup($"\n[grey]Tags:[/] {string.Join(" ", pkg.Tags.Take(10).Select(t => $"[blue]{Markup.Escape(t)}[/]"))}"));

        rows.Add(new Markup(""));
        rows.Add(new Markup($"[grey]View on nugetz:[/] [cyan]https://nugetz.dev/package/{Markup.Escape(pkg.Id)}[/]"));

        var panel = new Panel(new Rows(rows))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
            .Padding(1, 0, 1, 0);

        AnsiConsole.Write(panel);
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "—";
        var singleLine = text.ReplaceLineEndings(" ");
        return singleLine.Length <= maxLength ? singleLine : singleLine[..(maxLength - 1)] + "…";
    }
}
