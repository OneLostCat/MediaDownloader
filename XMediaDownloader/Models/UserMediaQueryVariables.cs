using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record UserMediaQueryVariables
{
    public required string UserId { get; set; }
    public required int Count { get; set; }
    public required bool IncludePromotedContent { get; set; }
    public required bool WithClientEventToken { get; set; }
    public required bool WithBirdwatchNotes { get; set; }
    public required bool WithVoice { get; set; }
    public required bool WithV2Timeline { get; set; }
    public required string Cursor { get; set; }
}

// Json 序列化
[JsonSerializable(typeof(UserMediaQueryVariables))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class UserMediaQueryVariablesContext : JsonSerializerContext;
