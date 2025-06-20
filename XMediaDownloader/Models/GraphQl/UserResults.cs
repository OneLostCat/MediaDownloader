namespace XMediaDownloader.Models.GraphQl;

public record UserResults
{
    public required UserResultInfo Result { get; set; }
}

public record UserResultInfo
{
    public required string Id { get; set; }
    public required string RestId { get; set; }
    public required UserLegacy Legacy { get; set; }
}

public record UserLegacy
{
    public required string ScreenName { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string CreatedAt { get; set; }
}
