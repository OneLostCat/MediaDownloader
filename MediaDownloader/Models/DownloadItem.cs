namespace MediaDownloader.Models;

public record DownloadItem(string Url, string Extension, int? Bitrate = null);
