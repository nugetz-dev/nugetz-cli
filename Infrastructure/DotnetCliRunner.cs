using System.Diagnostics;

namespace Nugetz.Cli.Infrastructure;

public sealed class DotnetCliRunner
{
    public async Task<(bool Success, string Output, string Error)> InstallPackageAsync(
        string projectPath, string packageName, string? version)
    {
        var args = $"add \"{projectPath}\" package {packageName}";
        if (version is not null)
            args += $" --version {version}";

        return await RunAsync(args);
    }

    public async Task<(bool Success, string Output, string Error)> PackAsync(string? projectPath)
    {
        var args = "pack -c Release -o ./nupkg";
        if (projectPath is not null)
            args += $" \"{projectPath}\"";
        return await RunAsync(args);
    }

    public async Task<(bool Success, string Output, string Error)> PushAsync(string nupkgPath, string apiKey)
    {
        var args = $"nuget push \"{nupkgPath}\" --api-key {apiKey} --source https://api.nuget.org/v3/index.json";
        return await RunAsync(args);
    }

    private static async Task<(bool Success, string Output, string Error)> RunAsync(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode == 0, output, error);
    }
}
