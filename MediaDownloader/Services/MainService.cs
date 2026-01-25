using MediaDownloader.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediaDownloader.Services;

public class MainService(
    CommandLineArguments args,
    ILogger<MainService> logger,
    StorageService storage,
    XApiService api,
    DownloadService download,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancel)
    {
        // 输出参数信息
        OutputArgumentsInfo();

        // 加载数据
        await storage.LoadAsync();

        try
        {
            if (!args.WithoutDownloadInfo)
            {
                // 获取用户
                var user = await api.GetUserByScreenNameAsync(args.Username, cancel);
                
                // 输出用户信息
                OutputUserInfo(user);
                
                // 获取媒体
                await api.GetUserMediaAsync(user.Id, cancel);
            }

            // 下载媒体
            if (!args.WithoutDownloadMedia)
            {
                await download.DownloadMediaAsync(cancel);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("操作取消");
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "错误");
        }

        // 保存数据
        await storage.SaveAsync();

        // 退出
        lifetime.StopApplication();
    }

    // 工具方法
    private void OutputArgumentsInfo()
    {
        logger.LogInformation("参数:");
        logger.LogInformation("  目标用户: {Username}", args.Username);
        logger.LogInformation("  Cookie 文件: {CookieFile}", args.CookieFile);
        logger.LogInformation("  输出目录: {OutputDir}", args.OutputDir);
        logger.LogInformation("  输出路径格式: {OutputPathFormat}", args.OutputPathFormat);
        logger.LogInformation("  目标媒体类型: {DownloadType}", args.DownloadType.HasFlag(MediaType.All) ? "All" : args.DownloadType);
        logger.LogInformation("  无需获取信息: {OnlyDownloadInfo}", args.WithoutDownloadInfo);
        logger.LogInformation("  无需下载媒体: {OnlyDownloadMedia}", args.WithoutDownloadMedia);
        logger.LogInformation("  工作目录: {WorkDir}", args.WorkDir);
        logger.LogInformation("  日志级别: {LogLevel}", args.LogLevel);
    }
    
    private void OutputUserInfo(User user)
    {
        logger.LogInformation("用户信息:");
        logger.LogInformation("  ID: {Id}", user.Id);
        logger.LogInformation("  名称: {Name}", user.Name);
        logger.LogInformation("  昵称: {Nickname}", user.Nickname);
        logger.LogInformation("  描述: {Description}", user.Description);
        logger.LogInformation("  注册时间: {CreationTime:yyyy-MM-dd HH:mm:ss}", user.CreationTime.LocalDateTime);
        logger.LogInformation("  媒体帖子数量: {MediaCount}", user.MediaTweetCount);
    }
}