namespace MediaDownloader.Models;

public record DownloadInfo
{
    public required string Path { get; set; }
    public required MediaDownloader Downloader { get; set; }
    public required string Url { get; set; }
}
