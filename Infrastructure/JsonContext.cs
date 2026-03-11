using System.Text.Json.Serialization;

namespace Nugetz.Cli.Infrastructure;

[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(PackageDetailInfo))]
[JsonSerializable(typeof(NugetzConfig))]
public sealed partial class NugetzJsonContext : JsonSerializerContext;

public sealed class NugetzConfig
{
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }
}
