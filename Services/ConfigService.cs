using System.Runtime.InteropServices;
using System.Text.Json;
using Nugetz.Cli.Infrastructure;

namespace Nugetz.Cli.Services;

public static class ConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nugetz");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    public static string? GetApiKey()
    {
        var envKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
            return envKey;

        var config = Load();
        return config?.ApiKey;
    }

    public static void SetApiKey(string key)
    {
        var config = Load() ?? new NugetzConfig();
        config.ApiKey = key;
        Save(config);
    }

    public static void RemoveApiKey()
    {
        if (!File.Exists(ConfigPath)) return;
        var config = Load() ?? new NugetzConfig();
        config.ApiKey = null;
        Save(config);
    }

    private static NugetzConfig? Load()
    {
        if (!File.Exists(ConfigPath)) return null;
        var json = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize(json, NugetzJsonContext.Default.NugetzConfig);
    }

    private static void Save(NugetzConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, NugetzJsonContext.Default.NugetzConfig);
        File.WriteAllText(ConfigPath, json);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            File.SetUnixFileMode(ConfigPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
