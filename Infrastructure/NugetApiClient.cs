using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Nugetz.Cli.Infrastructure;

public sealed class NugetApiClient
{
    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "Nugetz.Cli/0.2.0" } }
    };

    private const string NugetzApi = "https://nugetz.dev/api/cli";
    private const string NuGetSearch = "https://azuresearch-usnc.nuget.org/query";

    public async Task<SearchResponse> SearchAsync(string query, int skip = 0, int take = 10, bool prerelease = false)
    {
        // Try nugetz first, fall back to NuGet directly
        try
        {
            var url = $"{NugetzApi}/search?q={Uri.EscapeDataString(query)}&take={take}&prerelease={prerelease}";
            var response = await Http.GetFromJsonAsync(url, NugetzJsonContext.Default.SearchResponse);
            if (response is { Data.Count: > 0 }) return response;
        }
        catch { }

        var fallbackUrl = $"{NuGetSearch}?q={Uri.EscapeDataString(query)}&skip={skip}&take={take}&prerelease={prerelease}";
        var fallback = await Http.GetFromJsonAsync(fallbackUrl, NugetzJsonContext.Default.SearchResponse);
        return fallback ?? new SearchResponse();
    }

    public async Task<PackageInfo?> GetPackageInfoAsync(string packageId)
    {
        var search = await SearchAsync(packageId, take: 5);
        return search.Data.FirstOrDefault(p =>
            string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<PackageDetailInfo?> GetPackageDetailAsync(string packageId)
    {
        // Try enriched nugetz endpoint
        try
        {
            var url = $"{NugetzApi}/info?id={Uri.EscapeDataString(packageId)}";
            var res = await Http.GetAsync(url);
            if (res.IsSuccessStatusCode)
                return await res.Content.ReadFromJsonAsync(NugetzJsonContext.Default.PackageDetailInfo);
        }
        catch { }

        // Fall back to basic info from NuGet search
        var pkg = await GetPackageInfoAsync(packageId);
        if (pkg is null) return null;

        return new PackageDetailInfo
        {
            Id = pkg.Id,
            Version = pkg.Version,
            Description = pkg.Description,
            Authors = string.Join(", ", pkg.Owners),
            ProjectUrl = pkg.ProjectUrl,
            LicenseExpression = pkg.LicenseUrl,
            TotalDownloads = pkg.TotalDownloads,
            Verified = pkg.Verified,
            Tags = pkg.Tags,
        };
    }

    public async Task<List<string>> GetSuggestionsAsync(string query)
    {
        var search = await SearchAsync(query, take: 5);
        return search.Data.Select(p => p.Id).ToList();
    }
}

public sealed class SearchResponse
{
    [JsonPropertyName("totalHits")]
    public int TotalHits { get; set; }

    [JsonPropertyName("data")]
    public List<PackageInfo> Data { get; set; } = [];
}

public sealed class PackageInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("totalDownloads")]
    public long TotalDownloads { get; set; }

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    [JsonPropertyName("projectUrl")]
    public string? ProjectUrl { get; set; }

    [JsonPropertyName("licenseUrl")]
    public string? LicenseUrl { get; set; }

    [JsonPropertyName("owners")]
    public List<string> Owners { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("versions")]
    public List<PackageVersionInfo> Versions { get; set; } = [];
}

public sealed class PackageVersionInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("downloads")]
    public long Downloads { get; set; }
}

public sealed class PackageDetailInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("authors")]
    public string? Authors { get; set; }

    [JsonPropertyName("projectUrl")]
    public string? ProjectUrl { get; set; }

    [JsonPropertyName("licenseExpression")]
    public string? LicenseExpression { get; set; }

    [JsonPropertyName("totalDownloads")]
    public long TotalDownloads { get; set; }

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("published")]
    public string? Published { get; set; }

    [JsonPropertyName("dependencyCount")]
    public int DependencyCount { get; set; }

    [JsonPropertyName("targetFrameworks")]
    public List<string> TargetFrameworks { get; set; } = [];

    [JsonPropertyName("vulnerabilityCount")]
    public int VulnerabilityCount { get; set; }

    [JsonPropertyName("healthScore")]
    public HealthScoreInfo? HealthScore { get; set; }

    [JsonPropertyName("communitySignals")]
    public CommunitySignalsInfo? CommunitySignals { get; set; }
}

public sealed class HealthScoreInfo
{
    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("grade")]
    public string Grade { get; set; } = "";

    [JsonPropertyName("freshness")]
    public int Freshness { get; set; }

    [JsonPropertyName("popularity")]
    public int Popularity { get; set; }

    [JsonPropertyName("maintenance")]
    public int Maintenance { get; set; }

    [JsonPropertyName("security")]
    public int Security { get; set; }
}

public sealed class CommunitySignalsInfo
{
    [JsonPropertyName("stars")]
    public int Stars { get; set; }

    [JsonPropertyName("openIssues")]
    public int OpenIssues { get; set; }

    [JsonPropertyName("lastCommit")]
    public string LastCommit { get; set; } = "";

    [JsonPropertyName("contributors")]
    public int Contributors { get; set; }
}
