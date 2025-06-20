namespace XMediaDownloader.Models;

[Flags]
public enum DownloadType
{
    Image = 0b0001,
    Video = 0b0010,
    Gif = 0b0100,
    All = 0b0111,
}