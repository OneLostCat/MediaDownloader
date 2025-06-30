using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;

namespace XMediaDownloader;

public class DownloadService(
    ILogger<DownloadService> logger,
    CommandLineArguments args,
    StorageService storage,
    [FromKeyedServices("Download")] HttpClient httpClient)
{
    public async Task DownloadMediaAsync(string userId, CancellationToken cancel)
    {
        var user = storage.Content.Users[userId].Info;
        var tweets = storage.Content.Users[userId].Tweets;

        var totalMediaCount = tweets.Select(x => x.Value.Media.Count).Sum(); // 总媒体数量
        var tweetCount = 0; // 当前帖子数量
        var mediaCount = 0; // 当前媒体数量
        var downloadCount = 0; // 下载媒体数量

        logger.LogInformation("开始下载媒体");

        // 遍历帖子
        foreach (var tweet in tweets.Select(x => x.Value))
        {
            logger.LogInformation("下载媒体 {CreationTime:yyyy-MM-dd HH:mm:ss zzz} {Id}", tweet.CreationTime, tweet.Id);

            // 增加帖子计数
            tweetCount++;

            // 遍历媒体
            for (var i = 0; i < tweet.Media.Count; i++)
            {
                // 检查是否取消
                cancel.ThrowIfCancellationRequested();

                var media = tweet.Media[i];

                // 增加媒体计数
                mediaCount++;

                // 获取 Url 和扩展名
                string url;
                string extension;
                int? videoIndex = null;
                Video? video = null;

                if (media.Type != MediaType.Video) // 图片或动图
                {
                    // 获取原图 Url
                    var index = media.Url.LastIndexOf('.'); // 获取最后一个 "." 的位置

                    if (index == -1) throw new ArgumentException("无法获取原始图片 Url", media.Url);

                    var baseUrl = media.Url[..index]; // 获取基础 Url
                    extension = media.Url[(index + 1)..]; // 获取扩展名

                    url = $"{baseUrl}?format={extension}&name=orig";
                }
                else // 视频
                {
                    // 获取最高质量视频索引
                    videoIndex = media.Video
                        .Index()
                        .Where(x => x.Item.Bitrate != null)
                        .OrderByDescending(x => x.Item.Bitrate)
                        .First()
                        .Index;

                    // 获取视频
                    video = media.Video[(int)videoIndex];

                    // 获取视频 Url
                    url = video.Url;

                    // 获取拓展名
                    extension = new Uri(url).Segments.Last().Split('.').Last();
                }

                // 检查下载类型
                if (!args.DownloadType.HasFlag(media.Type))
                {
                    logger.LogInformation("  {Type} {Url} 跳过 ({mediaCount} / {totalMediaCount})", media.Type, url, mediaCount,
                        totalMediaCount);
                    continue;
                }

                // 获取文件信息
                var file = new FileInfo(Path.Combine(
                    args.OutputDir.ToString() != "." ? args.OutputDir.ToString() : "", // 避免使用默认目录时输出多余的 ".\"
                    PathBuilder.Build(
                        args.OutputPathFormat,
                        user.Id,
                        user.Name,
                        user.Nickname,
                        user.Description,
                        user.CreationTime,
                        user.MediaCount,
                        tweet.Id,
                        tweet.CreationTime,
                        tweet.Text,
                        tweet.Hashtags,
                        i + 1,
                        media.Type,
                        media.Url,
                        videoIndex,
                        video?.Url,
                        video?.Bitrate,
                        extension
                    )
                ));

                // 检查文件是否存在
                if (file.Exists)
                {
                    logger.LogInformation("  {Type} {Url} 文件已存在 {FilePath} ({mediaCount} / {totalMediaCount})", media.Type, url,
                        file, mediaCount, totalMediaCount);
                    continue;
                }

                logger.LogInformation("  {Type} {Url} -> {FilePath} ({mediaCount} / {totalMediaCount})", media.Type, url, file,
                    mediaCount, totalMediaCount);

                // 增加下载计数
                downloadCount++;

                // 发送请求
                var response = await httpClient.GetAsync(url, cancel);

                // 创建文件夹
                file.Directory?.Create(); // 无法判断是否为空

                // 写入临时文件
                var tempFile = new FileInfo(Path.GetTempFileName());

                await using (var fs = tempFile.Create())
                    await response.Content.CopyToAsync(fs, CancellationToken.None); // 不传递取消令牌，避免下载操作只执行一半

                // 移动文件
                tempFile.MoveTo(file.FullName);
            }
        }

        logger.LogInformation("媒体下载完成: 成功下载 {DownloadCount} / {TotalMediaCount}", downloadCount, totalMediaCount);
    }
}
