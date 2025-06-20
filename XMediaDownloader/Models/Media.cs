using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record Media
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("bitrate")]
    public long? Bitrate { get; set; }
}
