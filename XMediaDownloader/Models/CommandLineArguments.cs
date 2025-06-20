namespace XMediaDownloader.Models;

public record CommandLineArguments
{
    public required string User { get; init; }
    public required DownloadType DownloadType { get; init; }
    public required FileInfo CookieFile { get; init; }
    public required string Dir { get; init; }
    public required string Filename { get; init; }
}