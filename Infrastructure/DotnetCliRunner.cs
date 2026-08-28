using System.Diagnostics;

namespace Nugetz.Cli.Infrastructure;

public sealed record CommandResult(bool Success, string Output, string Error, int ExitCode);

public sealed class DotnetCliRunner
{
    public async Task<(bool Success, string Output, string Error)> InstallPackageAsync(
        string projectPath, string packageName, string? version)
    {
        var arguments = new List<string> { "add", projectPath, "package", packageName };
        if (version is not null)
            arguments.AddRange(["--version", version]);

        var result = await RunAsync(arguments);
        return (result.Success, result.Output, result.Error);
    }

    public async Task<(bool Success, string Output, string Error)> PackAsync(
        string? projectPath, string outputDirectory = "./nupkg")
    {
        var arguments = new List<string> { "pack", "-c", "Release", "-o", outputDirectory };
        if (projectPath is not null)
            arguments.Add(projectPath);
        var result = await RunAsync(arguments);
        return (result.Success, result.Output, result.Error);
    }

    public async Task<(bool Success, string Output, string Error)> PushAsync(
        string nupkgPath,
        string apiKey,
        string source = "https://api.nuget.org/v3/index.json",
        bool skipDuplicate = false)
    {
        var arguments = new List<string>
        {
            "nuget", "push", nupkgPath, "--api-key", apiKey, "--source", source,
        };
        if (skipDuplicate)
            arguments.Add("--skip-duplicate");
        var result = await RunAsync(arguments);
        return (result.Success, result.Output, result.Error);
    }

    public static async Task<CommandResult> RunAsync(IEnumerable<string> arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new CommandResult(
            process.ExitCode == 0,
            await outputTask,
            await errorTask,
            process.ExitCode);
    }
}
