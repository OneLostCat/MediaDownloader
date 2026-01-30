using MediaDownloader.Models;

namespace MediaDownloader.Downloaders;

public class JustForFansDownloader : IMediaDownloader
{
    public Task DownloadAsync(List<DownloadInfo> medias, CancellationToken cancel)
    {
        // TODO: 实现 JustForFans 下载逻辑
        
        return Task.CompletedTask;
    }
}
