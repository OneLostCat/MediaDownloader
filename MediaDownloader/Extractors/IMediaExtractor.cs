using MediaDownloader.Models;

namespace MediaDownloader.Extractors;

public interface IMediaExtractor
{
    public Task<List<DownloadInfo>> ExtractAsync(CancellationToken cancel);
}
