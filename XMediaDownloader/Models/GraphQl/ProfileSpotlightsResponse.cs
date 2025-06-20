using System.Text.Json.Serialization;

namespace XMediaDownloader.Models.GraphQl;

public record ProfileSpotlightsResponse
{
    public required UserResults UserResultByScreenName { get; set; }
}

// Json 序列化
[JsonSerializable(typeof(GraphQlResponse<ProfileSpotlightsResponse>))]
public partial class ProfileSpotlightsResponseContext : JsonSerializerContext;
