namespace Nugetz.Cli.Services;

public sealed class UpgradePlan
{
    public int SchemaVersion { get; init; } = 1;
    public required DateTimeOffset GeneratedAt { get; init; }
    public required string Status { get; set; }
    public bool Applied { get; set; }
    public List<UpgradeAction> Actions { get; init; } = [];
    public List<string> Issues { get; init; } = [];
}

public sealed class UpgradeAction
{
    public required string Project { get; init; }
    public required string Package { get; init; }
    public required string CurrentVersion { get; init; }
    public required string TargetVersion { get; init; }
    public bool MajorUpdate { get; init; }
    public string? CentralPackageFile { get; init; }
    public string Status { get; set; } = "planned";
    public string? Error { get; set; }
}
