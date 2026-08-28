using System.IO.Compression;
using System.Xml.Linq;

namespace Nugetz.Cli.Services;

public static class PackageValidator
{
    private static readonly string[] SensitiveSuffixes =
    [
        ".env", ".pfx", ".p12", ".key", ".pem", "appsettings.production.json",
    ];

    public static PackageValidationReport Validate(string packagePath)
    {
        var fullPath = Path.GetFullPath(packagePath);
        var report = new PackageValidationReport
        {
            PackagePath = fullPath,
            SizeBytes = new FileInfo(fullPath).Length,
        };

        try
        {
            using var archive = ZipFile.OpenRead(fullPath);
            var nuspecEntry = archive.Entries.FirstOrDefault(entry =>
                entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
            if (nuspecEntry is null)
            {
                Error(report, "missing-nuspec", "The package does not contain a .nuspec manifest.");
                return Complete(report);
            }

            using var nuspecStream = nuspecEntry.Open();
            var document = XDocument.Load(nuspecStream);
            var metadata = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "metadata");
            if (metadata is null)
            {
                Error(report, "missing-metadata", "The .nuspec manifest has no metadata element.");
                return Complete(report);
            }

            report.PackageId = Value(metadata, "id");
            report.Version = Value(metadata, "version");
            Require(report, report.PackageId, "id", "Package ID");
            Require(report, report.Version, "version", "Version");
            Require(report, Value(metadata, "authors"), "authors", "Authors");
            Require(report, Value(metadata, "description"), "description", "Description");

            var license = Element(metadata, "license");
            if (license is null && string.IsNullOrWhiteSpace(Value(metadata, "licenseUrl")))
                Error(report, "missing-license", "Add a license expression or packaged license file.");
            if (license?.Attribute("type")?.Value == "file" && !ContainsEntry(archive, license.Value))
                Error(report, "missing-license-file", $"The declared license file '{license.Value}' is not in the package.");

            ValidatePackagedAsset(report, archive, metadata, "readme", "missing-readme", "README");
            ValidatePackagedAsset(report, archive, metadata, "icon", "missing-icon", "icon");

            var repository = Element(metadata, "repository");
            if (repository is null ||
                (string.IsNullOrWhiteSpace(repository.Value) && string.IsNullOrWhiteSpace(repository.Attribute("url")?.Value)))
                Warn(report, "missing-repository", "Add repository metadata so customers can inspect the source and commit.");
            if (string.IsNullOrWhiteSpace(Value(metadata, "releaseNotes")))
                Warn(report, "missing-release-notes", "Add release notes or a release-notes URL for upgrade decisions.");

            report.AssemblyCount = archive.Entries.Count(entry =>
                (entry.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) ||
                 entry.FullName.StartsWith("ref/", StringComparison.OrdinalIgnoreCase) ||
                 entry.FullName.StartsWith("tools/", StringComparison.OrdinalIgnoreCase)) &&
                entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
            report.DependencyCount = metadata.Descendants().Count(element => element.Name.LocalName == "dependency");

            foreach (var entry in archive.Entries)
            {
                var normalized = entry.FullName.ToLowerInvariant();
                if (SensitiveSuffixes.Any(suffix => normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
                    Error(report, "sensitive-file", $"Potentially sensitive file included: {entry.FullName}");
            }

            if (report.SizeBytes > 50L * 1024 * 1024)
                Warn(report, "large-package", "The package is larger than 50 MB; confirm all packaged assets are intentional.");
        }
        catch (InvalidDataException exception)
        {
            Error(report, "invalid-archive", $"The .nupkg is not a valid ZIP archive: {exception.Message}");
        }
        catch (System.Xml.XmlException exception)
        {
            Error(report, "invalid-nuspec", $"The .nuspec XML is invalid: {exception.Message}");
        }

        return Complete(report);
    }

    private static void ValidatePackagedAsset(
        PackageValidationReport report,
        ZipArchive archive,
        XElement metadata,
        string elementName,
        string code,
        string label)
    {
        var path = Value(metadata, elementName);
        if (string.IsNullOrWhiteSpace(path))
        {
            Warn(report, code, $"Add a packaged {label} for a better NuGet gallery experience.");
            return;
        }
        if (!ContainsEntry(archive, path))
            Error(report, code, $"The declared {label} file '{path}' is not in the package.");
    }

    private static bool ContainsEntry(ZipArchive archive, string path) =>
        archive.Entries.Any(entry => entry.FullName.Equals(path.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));

    private static XElement? Element(XElement metadata, string localName) =>
        metadata.Elements().FirstOrDefault(element => element.Name.LocalName == localName);

    private static string? Value(XElement metadata, string localName) =>
        Element(metadata, localName)?.Value.Trim();

    private static void Require(
        PackageValidationReport report,
        string? value,
        string code,
        string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            Error(report, $"missing-{code}", $"{label} is required.");
    }

    private static void Error(PackageValidationReport report, string code, string message) =>
        report.Issues.Add(new PackageValidationIssue { Severity = "error", Code = code, Message = message });

    private static void Warn(PackageValidationReport report, string code, string message) =>
        report.Issues.Add(new PackageValidationIssue { Severity = "warning", Code = code, Message = message });

    private static PackageValidationReport Complete(PackageValidationReport report)
    {
        report.Status = report.Issues.Any(issue => issue.Severity == "error")
            ? "invalid"
            : report.Issues.Count > 0 ? "warnings" : "valid";
        return report;
    }
}
