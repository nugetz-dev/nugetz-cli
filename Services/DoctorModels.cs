using System.Text.Json.Serialization;

namespace Nugetz.Cli.Services;

public sealed class DoctorReport
{
    public int SchemaVersion { get; init; } = 1;
    public required DateTimeOffset GeneratedAt { get; init; }
    public required string Root { get; init; }
    public required string Status { get; set; }
    public List<string> Sources { get; init; } = [];
    public List<DoctorProject> Projects { get; init; } = [];
    public DoctorSummary Summary { get; init; } = new();
    public List<string> Issues { get; init; } = [];
}

public sealed class DoctorSummary
{
    public int ProjectCount { get; set; }
    public int FrameworkCount { get; set; }
    public int PackageCount { get; set; }
    public int OutdatedCount { get; set; }
    public int VulnerableCount { get; set; }
    public int UnknownProjectCount { get; set; }
}

public sealed class DoctorProject
{
    public required string Path { get; init; }
    public required string Status { get; set; }
    public string? Error { get; set; }
    public List<DoctorFramework> Frameworks { get; init; } = [];
    public List<DependencyPathResult> DependencyPaths { get; init; } = [];
}

public sealed class DoctorFramework
{
    public required string Framework { get; init; }
    public List<DoctorPackage> Packages { get; init; } = [];
}

public sealed class DoctorPackage
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public string? RequestedVersion { get; set; }
    public required string ResolvedVersion { get; set; }
    public string? LatestVersion { get; set; }
    public string UpdateStatus { get; set; } = "current";
    public string VulnerabilityStatus { get; set; } = "verified";
    public List<DoctorVulnerability> Vulnerabilities { get; init; } = [];

    [JsonIgnore]
    public bool IsOutdated => LatestVersion is not null &&
        !string.Equals(ResolvedVersion, LatestVersion, StringComparison.OrdinalIgnoreCase);
}

public sealed class DoctorVulnerability
{
    public required string Severity { get; init; }
    public required string AdvisoryUrl { get; init; }
}

public sealed class DependencyPathResult
{
    public required string Package { get; init; }
    public required string Status { get; init; }
    public required string Output { get; init; }
}

internal sealed class PackageListSnapshot
{
    public List<string> Sources { get; } = [];
    public List<DoctorProject> Projects { get; } = [];
}
