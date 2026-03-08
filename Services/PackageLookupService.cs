using Nugetz.Cli.Infrastructure;

namespace Nugetz.Cli.Services;

public sealed class PackageLookupService(NugetApiClient api)
{
    public async Task<PackageInfo?> ResolvePackageAsync(string packageName)
    {
        return await api.GetPackageInfoAsync(packageName);
    }

    public async Task<PackageDetailInfo?> GetDetailAsync(string packageId)
    {
        return await api.GetPackageDetailAsync(packageId);
    }

    public async Task<List<string>> GetSuggestionsAsync(string query)
    {
        return await api.GetSuggestionsAsync(query);
    }

    public async Task<SearchResponse> SearchAsync(string query, int take = 10, bool prerelease = false)
    {
        return await api.SearchAsync(query, take: take, prerelease: prerelease);
    }
}
