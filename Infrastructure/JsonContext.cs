using System.Text.Json.Serialization;

namespace Nugetz.Cli.Infrastructure;

[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(PackageDetailInfo))]
public sealed partial class NugetzJsonContext : JsonSerializerContext;
