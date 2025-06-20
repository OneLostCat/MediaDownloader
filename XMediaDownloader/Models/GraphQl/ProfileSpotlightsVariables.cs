using System.Text.Json.Serialization;

namespace XMediaDownloader.Models.GraphQl;

public record ProfileSpotlightsVariables
{
    [JsonPropertyName("screen_name")]
    public required string ScreenName { get; set; }
}

// Json 序列化
[JsonSerializable(typeof(ProfileSpotlightsVariables))]
public partial class ProfileSpotlightsVariablesContext : JsonSerializerContext;
