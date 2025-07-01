using System.Text.Json.Serialization;

namespace XMediaDownloader.Models;

public record StorageContent
{
    // 降序排列
    public static readonly Comparer<string> IdComparer = Comparer<string>.Create((a, b) =>
        string.Compare(b, a, StringComparison.Ordinal));
    
    public Dictionary<string, UserData> Users { get; set; } = new();
}

public record UserData
{
    public required User Info { get; set; }
    public SortedDictionary<string, Tweet> Tweets { get; set; } = new(StorageContent.IdComparer);
    public string? CurrentCursor { get; set; }
}

// Json 序列化
[JsonSerializable(typeof(StorageContent))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
public partial class StorageContentContext : JsonSerializerContext;
