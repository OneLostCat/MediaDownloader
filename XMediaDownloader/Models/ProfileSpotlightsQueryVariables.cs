using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record ProfileSpotlightsQueryVariables
{
    [JsonPropertyName("screen_name")]
    public required string ScreenName { get; set; }
}

// Json 序列化
[JsonSerializable(typeof(ProfileSpotlightsQueryVariables))]
public partial class ProfileSpotlightsQueryVariablesContext : JsonSerializerContext;
