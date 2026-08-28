namespace Nugetz.Cli.Services;

public sealed class PackageValidationReport
{
    public int SchemaVersion { get; init; } = 1;
    public required string PackagePath { get; init; }
    public string? PackageId { get; set; }
    public string? Version { get; set; }
    public long SizeBytes { get; set; }
    public int AssemblyCount { get; set; }
    public int DependencyCount { get; set; }
    public string Status { get; set; } = "valid";
    public List<PackageValidationIssue> Issues { get; init; } = [];
}

public sealed class PackageValidationIssue
{
    public required string Severity { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }
}
