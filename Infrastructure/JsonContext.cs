using System.Text.Json.Serialization;
using Nugetz.Cli.Services;

namespace Nugetz.Cli.Infrastructure;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(PackageDetailInfo))]
[JsonSerializable(typeof(NugetzConfig))]
[JsonSerializable(typeof(DoctorReport))]
[JsonSerializable(typeof(UpgradePlan))]
[JsonSerializable(typeof(PackageValidationReport))]
public sealed partial class NugetzJsonContext : JsonSerializerContext;

public sealed class NugetzConfig
{
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }
}
