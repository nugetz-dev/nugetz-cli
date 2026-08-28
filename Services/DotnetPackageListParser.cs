using System.Text.Json;

namespace Nugetz.Cli.Services;

internal static class DotnetPackageListParser
{
    public static PackageListSnapshot Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var snapshot = new PackageListSnapshot();

        if (root.TryGetProperty("sources", out var sources))
            foreach (var source in sources.EnumerateArray())
                if (source.GetString() is { Length: > 0 } value)
                    snapshot.Sources.Add(value);

        if (!root.TryGetProperty("projects", out var projects))
            return snapshot;

        foreach (var projectElement in projects.EnumerateArray())
        {
            var project = new DoctorProject
            {
                Path = GetString(projectElement, "path") ?? "unknown",
                Status = "verified",
            };

            if (projectElement.TryGetProperty("frameworks", out var frameworks))
            {
                foreach (var frameworkElement in frameworks.EnumerateArray())
                {
                    var framework = new DoctorFramework
                    {
                        Framework = GetString(frameworkElement, "framework") ?? "unknown",
                    };
                    ReadPackages(frameworkElement, "topLevelPackages", "top-level", framework.Packages);
                    ReadPackages(frameworkElement, "transitivePackages", "transitive", framework.Packages);
                    project.Frameworks.Add(framework);
                }
            }

            snapshot.Projects.Add(project);
        }

        return snapshot;
    }

    private static void ReadPackages(
        JsonElement framework,
        string propertyName,
        string type,
        List<DoctorPackage> packages)
    {
        if (!framework.TryGetProperty(propertyName, out var packageElements))
            return;

        foreach (var packageElement in packageElements.EnumerateArray())
        {
            var id = GetString(packageElement, "id");
            var resolved = GetString(packageElement, "resolvedVersion");
            if (id is null || resolved is null)
                continue;

            var package = new DoctorPackage
            {
                Id = id,
                Type = type,
                RequestedVersion = GetString(packageElement, "requestedVersion"),
                ResolvedVersion = resolved,
                LatestVersion = GetString(packageElement, "latestVersion"),
            };

            if (packageElement.TryGetProperty("vulnerabilities", out var vulnerabilities))
            {
                foreach (var vulnerability in vulnerabilities.EnumerateArray())
                {
                    package.Vulnerabilities.Add(new DoctorVulnerability
                    {
                        Severity = GetString(vulnerability, "severity") ?? "unknown",
                        AdvisoryUrl = GetString(vulnerability, "advisoryUrl") ??
                            GetString(vulnerability, "advisoryurl") ?? "unknown",
                    });
                }
            }

            packages.Add(package);
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) ? property.ToString() : null;
}
