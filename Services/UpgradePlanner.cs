namespace Nugetz.Cli.Services;

public static class UpgradePlanner
{
    public static UpgradePlan Build(
        DoctorReport report,
        string? packageFilter,
        string? requestedVersion)
    {
        var plan = new UpgradePlan
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Status = report.Status,
        };

        var candidates = report.Projects
            .Where(project => project.Status != "unavailable")
            .SelectMany(project => project.Frameworks.SelectMany(framework => framework.Packages)
                .Where(package => package.Type == "top-level")
                .Select(package => (Project: project.Path, Package: package)))
            .Where(candidate => packageFilter is null ||
                candidate.Package.Id.Equals(packageFilter, StringComparison.OrdinalIgnoreCase))
            .GroupBy(candidate => $"{candidate.Project}\0{candidate.Package.Id}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        foreach (var candidate in candidates)
        {
            var target = requestedVersion ?? candidate.Package.LatestVersion;
            if (target is null || target.Equals(candidate.Package.ResolvedVersion, StringComparison.OrdinalIgnoreCase))
                continue;

            plan.Actions.Add(new UpgradeAction
            {
                Project = candidate.Project,
                Package = candidate.Package.Id,
                CurrentVersion = candidate.Package.ResolvedVersion,
                TargetVersion = target,
                MajorUpdate = IsMajorUpdate(candidate.Package.ResolvedVersion, target),
                CentralPackageFile = FindCentralPackageFile(candidate.Project),
            });
        }

        if (packageFilter is not null && plan.Actions.Count == 0)
            plan.Issues.Add(
                $"No upgradable top-level reference to {packageFilter} was found. Use 'nugetz doctor --why {packageFilter}' if it is transitive.");
        return plan;
    }

    public static bool IsMajorUpdate(string currentVersion, string targetVersion)
    {
        static int? Major(string version)
        {
            var first = version.TrimStart('[', '(').Split('.', '-', '+')[0];
            return int.TryParse(first, out var major) ? major : null;
        }

        var current = Major(currentVersion);
        var target = Major(targetVersion);
        return current is not null && target is not null && target > current;
    }

    private static string? FindCentralPackageFile(string projectPath)
    {
        var directory = Directory.GetParent(projectPath);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Directory.Packages.props");
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }
        return null;
    }
}
