using Spectre.Console;

namespace Nugetz.Cli.UI;

public static class Output
{
    public static void Success(string message) =>
        AnsiConsole.MarkupLine($"[green]✓[/] {message}");

    public static void Error(string message) =>
        AnsiConsole.MarkupLine($"[red]✗[/] {message}");

    public static void Warning(string message) =>
        AnsiConsole.MarkupLine($"[yellow]![/] {message}");

    public static void Info(string message) =>
        AnsiConsole.MarkupLine($"[blue]>[/] {message}");

    public static void Muted(string message) =>
        AnsiConsole.MarkupLine($"[grey]{message}[/]");

    public static string FormatDownloads(long n) => n switch
    {
        >= 1_000_000_000 => $"{n / 1_000_000_000.0:F1}B",
        >= 1_000_000 => $"{n / 1_000_000.0:F1}M",
        >= 1_000 => $"{n / 1_000.0:F1}K",
        _ => n.ToString()
    };

    public static string TimeAgo(string? dateStr)
    {
        if (dateStr is null) return "unknown";
        if (!DateTime.TryParse(dateStr, out var date)) return "unknown";
        var days = (int)(DateTime.UtcNow - date).TotalDays;
        return days switch
        {
            0 => "today",
            1 => "1 day ago",
            < 30 => $"{days} days ago",
            < 365 => $"{days / 30} months ago",
            _ => $"{days / 365} years ago"
        };
    }
}
