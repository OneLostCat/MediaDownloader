using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record Tweet
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    [JsonPropertyName("user_id")]
    public required string UserId { get; set; }

    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [JsonPropertyName("hashtags")]
    public List<string> Hashtags { get; set; } = [];

    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; set; }

    [JsonPropertyName("media")]
    public List<Media> Media { get; set; } = [];
}