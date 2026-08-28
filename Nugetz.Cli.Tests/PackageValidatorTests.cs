using System.IO.Compression;
using Nugetz.Cli.Services;

namespace Nugetz.Cli.Tests;

public sealed class PackageValidatorTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"nugetz-package-tests-{Guid.NewGuid():N}");

    [Fact]
    public void RejectsDeclaredFilesThatAreMissingFromThePackage()
    {
        var package = CreatePackage(
            """
            <?xml version="1.0"?>
            <package><metadata>
              <id>Example.Safe</id><version>1.0.0</version><authors>Nugetz</authors>
              <description>Example package</description><license type="expression">MIT</license>
              <readme>README.md</readme><icon>icon.png</icon>
            </metadata></package>
            """);

        var report = PackageValidator.Validate(package);

        Assert.Equal("invalid", report.Status);
        Assert.Contains(report.Issues, issue => issue.Code == "missing-readme" && issue.Severity == "error");
        Assert.Contains(report.Issues, issue => issue.Code == "missing-icon" && issue.Severity == "error");
    }

    [Fact]
    public void RejectsPotentiallySensitiveFiles()
    {
        var package = CreatePackage(
            """
            <?xml version="1.0"?>
            <package><metadata>
              <id>Example.Unsafe</id><version>1.0.0</version><authors>Nugetz</authors>
              <description>Example package</description><license type="expression">MIT</license>
            </metadata></package>
            """,
            ".env");

        var report = PackageValidator.Validate(package);

        Assert.Contains(report.Issues, issue => issue.Code == "sensitive-file" && issue.Severity == "error");
    }

    private string CreatePackage(string nuspec, params string[] entries)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, $"{Guid.NewGuid():N}.nupkg");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        var nuspecEntry = archive.CreateEntry("Example.nuspec");
        using (var writer = new StreamWriter(nuspecEntry.Open()))
            writer.Write(nuspec);
        foreach (var name in entries)
            archive.CreateEntry(name);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
