using System.Text.Json;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;
using XMediaDownloader.Models.GraphQl;

namespace XMediaDownloader;

public class StorageService(ILogger<StorageService> logger) : IAsyncDisposable
{
    private const string FilePath = "storage.json";

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        TypeInfoResolver = StorageContentContext.Default,
        // Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public StorageContent Content { get; set; } = new();

    public async Task SaveAsync()
    {
        try
        {
            // 创建目录
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath) ?? "");

            // 打开文件 (使用 File.Open 是因为 File.WriteAllText 无法写入隐藏文件)
            await using var fileStream = File.OpenWrite(FilePath);

            // 序列化并写入文件
#pragma warning disable IL2026
#pragma warning disable IL3050
            await JsonSerializer.SerializeAsync(fileStream, _jsonSerializerOptions);
#pragma warning restore IL3050
#pragma warning restore IL2026
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "数据保存失败");
            return;
        }

        logger.LogDebug("数据保存成功");
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(FilePath))
        {
            logger.LogDebug("数据文件不存在，无法加载数据");
            return;
        }

        StorageContent? data;

        try
        {
            // 打开文件
            await using var fileStream = File.OpenRead(FilePath);

            // 反序列化
#pragma warning disable IL2026
#pragma warning disable IL3050
            data = await JsonSerializer.DeserializeAsync<StorageContent>(fileStream, _jsonSerializerOptions);
#pragma warning restore IL3050
#pragma warning restore IL2026
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "数据加载失败");
            return;
        }

        if (data == null)
        {
            logger.LogError("数据加载失败");
            return;
        }

        logger.LogDebug("数据加载成功");
        Content = data;
    }

    public void AddUserData(string userId, UserData newData)
    {
        var data = Content.Data.GetValueOrDefault(userId) ?? new UserData();

        data.CurrentCursor = newData.CurrentCursor;

        foreach (var user in newData.Users)
        {
            // 创建用户
            if (data.Users.TryAdd(user.Key, user.Value)) continue;

            // 合并用户历史
            foreach (var pair in user.Value.UserHistory)
            {
                data.Users[userId].UserHistory[pair.Key] = pair.Value;
            }

            // 合并推文
            foreach (var tweet in user.Value.Tweets)
            {
                data.Users[userId].Tweets[tweet.Key] = tweet.Value;
            }
        }
    }

    public void AddTweets(string userId, string cursor, List<Tweet> tweets)
    {
        // 处理推文
        var newData = new UserData { CurrentCursor = cursor, Users = new Dictionary<string, UserDataItem>() };

        var users = Content.Users;

        foreach (var tweet in tweets)
        {
            // 使用 Tweet 的用户 ID 找到对应的用户信息
            var tweetUserId = tweet.UserId ?? throw new Exception("用户 ID 为 null");

            if (!users.TryGetValue(tweetUserId, out var user)) continue;

            if (!newData.Users.TryGetValue(tweetUserId, out var userDataItem))
            {
                userDataItem = new UserDataItem { UserHistory = new Dictionary<string, User> { [user.Id] = user }, };

                newData.Users[tweetUserId] = userDataItem;
            }
            
            userDataItem.Tweets[tweet.Id] = tweet;
        }

        // 合并推文
        var data = Content.Data.GetValueOrDefault(userId) ?? new UserData();

        data.CurrentCursor = newData.CurrentCursor;

        foreach (var (newUserId, newUserData) in newData.Users)
        {
            // 创建用户
            if (!data.Users.TryGetValue(newUserId, out var userDataItem))
            {
                data.Users[newUserId] = newUserData;
                continue;
            }

            // 合并用户历史
            foreach (var pair in newUserData.UserHistory)
            {
                userDataItem.UserHistory[pair.Key] = pair.Value;
            }

            // 合并推文
            foreach (var pair in newUserData.Tweets)
            {
                data.Users[newUserId].Tweets[pair.Key] = pair.Value;
            }
        }
    }

    public void UpdateUserInfo(UserResultInfo userInfo)
    {
        if (userInfo.RestId == null) throw new Exception("无法获取目标用户 ID");

        Content.Users[userInfo.RestId] = new User
        {
            Id = userInfo.RestId,
            ScreenName = userInfo.Legacy.ScreenName,
            Name = userInfo.Legacy.Name,
            Description = userInfo.Legacy.Description,
            CreatedTime = DateTime.UtcNow
        };
    }

    public async ValueTask DisposeAsync()
    {
        await SaveAsync();

        GC.SuppressFinalize(this);
    }
}