using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using XMediaDownloader.Models;
using XMediaDownloader.Models.GraphQl;

namespace XMediaDownloader;

public class XApiService(ILogger<XApiService> logger, IHttpClientFactory httpClientFact, StorageService storage)
{
    // API 信息
    public const string BaseUrl = "https://x.com";
    public const string ProfileSpotlightsUrl = "/i/api/graphql/-0XdHI-mrHWBQd8-oLo1aA/ProfileSpotlightsQuery";
    public const string TweetDetailUrl = "/i/api/graphql/tivxwX7ezCWlYBkrhxoR0A/TweetDetail";
    public const string UserMediaUrl = "/i/api/graphql/BGmkmGDG0kZPM-aoQtNTTw/UserMedia";
    public const string LikeUrl = "/i/api/graphql/oLLzvV4gwmdq_nhPM4cLwg/Likes";
    public const string BookmarkUrl = "/i/api/graphql/Ds7FCVYEIivOKHsGcE84xQ/Bookmarks";

    public const string Bearer =
        "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA";

    public const string UserMediaFeatures =
        "{\"profile_label_improvements_pcf_label_in_post_enabled\":false," +
        "\"rweb_tipjar_consumption_enabled\":true," +
        "\"responsive_web_graphql_exclude_directive_enabled\":true," +
        "\"verified_phone_label_enabled\":false," +
        "\"creator_subscriptions_tweet_preview_api_enabled\":true," +
        "\"responsive_web_graphql_timeline_navigation_enabled\":true," +
        "\"responsive_web_graphql_skip_user_profile_image_extensions_enabled\":false," +
        "\"premium_content_api_read_enabled\":false," +
        "\"communities_web_enable_tweet_community_results_fetch\":true," +
        "\"c9s_tweet_anatomy_moderator_badge_enabled\":true," +
        "\"responsive_web_grok_analyze_button_fetch_trends_enabled\":false," +
        "\"responsive_web_grok_analyze_post_followups_enabled\":true," +
        "\"responsive_web_grok_share_attachment_enabled\":true," +
        "\"articles_preview_enabled\":true," +
        "\"responsive_web_edit_tweet_api_enabled\":true," +
        "\"graphql_is_translatable_rweb_tweet_is_translatable_enabled\":true," +
        "\"view_counts_everywhere_api_enabled\":true," +
        "\"longform_notetweets_consumption_enabled\":true," +
        "\"responsive_web_twitter_article_tweet_consumption_enabled\":true," +
        "\"tweet_awards_web_tipping_enabled\":false," +
        "\"creator_subscriptions_quote_tweet_preview_enabled\":false," +
        "\"freedom_of_speech_not_reach_fetch_enabled\":true," +
        "\"standardized_nudges_misinfo\":true," +
        "\"tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled\":true," +
        "\"rweb_video_timestamps_enabled\":true," +
        "\"longform_notetweets_rich_text_read_enabled\":true," +
        "\"longform_notetweets_inline_media_enabled\":true," +
        "\"responsive_web_enhance_cards_enabled\":false}";

    private readonly HttpClient _httpClient = httpClientFact.CreateClient("X");

    // 公开方法
    public async Task<User> GetUserAsync(string username, CancellationToken cancel)
    {
        logger.LogInformation("获取用户信息: {Username}", username);

        // 参数
        var variables = JsonSerializer.Serialize(new ProfileSpotlightsVariables { ScreenName = username },
            ProfileSpotlightsVariablesContext.Default.ProfileSpotlightsVariables);

        // 请求
        var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(ProfileSpotlightsUrl, variables));

        // 发送请求
        var response = await _httpClient.SendAsync(request, cancel);

        // 解析响应
        var content = await response.Content.ReadFromJsonAsync<GraphQlResponse<ProfileSpotlightsResponse>>(
            ProfileSpotlightsResponseContext.Default.GraphQlResponseProfileSpotlightsResponse, cancel);

        if (content?.Data?.UserResultByScreenName.Result == null) throw new Exception("无法获取用户信息");

        // 生成用户信息
        return GetUserInfo(content.Data.UserResultByScreenName.Result);
    }

    public async Task GetMediaAsync(string userId, CancellationToken cancel)
    {
        // 加载数据
        await storage.LoadAsync();
        var cursor = storage.Content.Data[userId].CurrentCursor;

        while (true)
        {
            var (nextCursor, newTweets) = await GetMediaAsync(userId, cursor, 20, cancel);

            if (newTweets.Count == 0) break;

            // 储存帖子
            storage.AddTweets(userId, nextCursor, newTweets);
            await storage.SaveAsync();

            cursor = nextCursor;

            await Task.Delay(1000, cancel); // 等待避免封号
        }
    }

    // 工具方法
    private User GetUserInfo(UserResultInfo userInfo) => new()
    {
        Id = userInfo.RestId,
        ScreenName = userInfo.Legacy.ScreenName,
        Name = userInfo.Legacy.Name,
        Description = userInfo.Legacy.Description,
        CreatedTime = DateTime.UtcNow
    };

    private async Task<(string, List<Tweet>)> GetMediaAsync(string userId, string cursor, int count, CancellationToken cancel)
    {
        // 参数
        var variables = JsonSerializer.Serialize(new UserMediaVariables
        {
            UserId = userId,
            Count = count,
            IncludePromotedContent = false,
            WithClientEventToken = false,
            WithBirdwatchNotes = false,
            WithVoice = true,
            WithV2Timeline = true,
            Cursor = cursor
        }, UserMediaVariablesContext.Default.UserMediaVariables);

        // 请求
        var request = new HttpRequestMessage(HttpMethod.Get,
            BuildUrl(UserMediaUrl, variables, UserMediaFeatures, "{\"withArticlePlainText\":false}"));

        // 发送请求
        var response = await _httpClient.SendAsync(request, cancel);

        // 解析
        var content =
            await response.Content.ReadFromJsonAsync<GraphQlResponse<UserMediaResponse>>(
                UserMediaResponseContext.Default.GraphQlResponseUserMediaResponse, cancel);

        // 获取帖子
        var newTweets = new List<Tweet>();
        var nextCursor = cursor;

        foreach (var instruction in content?.Data?.User.Result.TimelineV2.Timeline.Instructions ??
                                    throw new Exception("指令不能为 null"))
        {
            if (instruction.Type == "TimelineAddEntries")
            {
                foreach (var entry in instruction.Entries)
                {
                    if (entry.EntryId.StartsWith("profile-"))
                    {
                        var tweet = ProcessTweetMedia(entry);

                        if (tweet.Count != 0)
                        {
                            newTweets.AddRange(tweet);
                        }
                    }
                    else if (entry.EntryId.StartsWith("cursor-bottom-"))
                    {
                        nextCursor = entry.Content.Value;
                    }
                }
            }
            else if (instruction.Type == "TimelineAddToModule")
            {
                newTweets.AddRange(instruction.ModuleItems
                    .ToAsyncEnumerable()
                    .Where(entry => entry.EntryId.StartsWith("profile-"))
                    .SelectAwait(ProcessTweetMediaSingle)
                    .ToEnumerable()
                );
            }
        }

        return (nextCursor, newTweets);
    }

    private string BuildUrl(string endpoint, string variables, string? features = null, string? fieldToggles = null)
    {
        var sb = new StringBuilder(BaseUrl + endpoint + $"?variables={variables}");

        if (features != null)
        {
            sb.Append($"&features={features}");
        }

        if (fieldToggles != null)
        {
            sb.Append($"&fieldToggles={fieldToggles}");
        }

        return sb.ToString();
    }

    private List<Tweet> ProcessTweetMedia(TimelineEntry entry)
    {
        var tweets = new List<Tweet>();

        if (entry.Content.Items.Count == 0) return tweets;

        foreach (var item in entry.Content.Items)
        {
            // if (item.Item.ItemContent.TweetResults.Result == null) continue;

            var tweetResult = item.Item.ItemContent.TweetResults.Result;

            // if (tweetResult.Tombstone != null) continue;

            var userInfo = tweetResult.Core.UserResults.Result;

            // if (userInfo.RestId == null) throw new Exception("用户 ID 不能为 null");

            // 储存用户信息
            storage.Content.Users[userInfo.RestId] = GetUserInfo(userInfo);

            var tweet = new Tweet
            {
                Id = tweetResult.RestId,
                UserId = userInfo.RestId,
                Text = tweetResult.Legacy.FullText ,
                Hashtags = tweetResult.Legacy.Entities.Hashtags.Select(x => x.Text).ToList(),
                CreatedAt = tweetResult.Legacy.CreatedAt,
                Media = ProcessMedia(tweetResult.Legacy.ExtendedEntities.Media)
            };

            tweets.Add(tweet);
        }

        return tweets;
    }

    private async ValueTask<Tweet> ProcessTweetMediaSingle(ItemMedia entry)
    {
        var tweetResult = entry.Item.ItemContent.TweetResults.Result;

        // if (tweetResult == null) return null;

        var userInfo = tweetResult.Core.UserResults.Result;

        // if (userInfo.RestId == null) throw new Exception("用户 ID 不能为 null");

        // 储存用户信息
        storage.Content.Users[userInfo.RestId] = GetUserInfo(userInfo);
        await storage.SaveAsync();

        return new Tweet
        {
            Id = tweetResult.RestId,
            UserId = userInfo.RestId,
            Text = tweetResult.Legacy.FullText,
            Hashtags = tweetResult.Legacy.Entities.Hashtags.Select(h => h.Text).ToList(),
            CreatedAt = tweetResult.Legacy.CreatedAt,
            Media = ProcessMedia(tweetResult.Legacy.Entities.Media)
        };
    }

    private List<TweetMedia> ProcessMedia(List<MediaEntity> mediaEntities)
    {
        if (mediaEntities.Count == 0) return [];

        return mediaEntities.Select(x => new TweetMedia
        {
            Type = x.Type,
            Url = x.Type == "photo" ? GetOriginalImageUrl(x.MediaUrlHttps) : GetHighestQualityVideoUrl(x.VideoInfo),
            Bitrate = x.Type == "video" ? GetHighestBitrate(x.VideoInfo) : null
        }).ToList();
    }

    private string GetOriginalImageUrl(string url)
    {
        var parts = url.Split('.');
        var ext = parts.Last();
        var basePath = string.Join(".", parts.Take(parts.Length - 1));

        return $"{basePath}?format={ext}&name=orig";
    }

    private string GetHighestQualityVideoUrl(VideoInfo videoInfo) => videoInfo.Variants
        .Where(x => x.Bitrate.HasValue)
        .OrderByDescending(x => x.Bitrate)
        .First()
        .Url;

    private long? GetHighestBitrate(VideoInfo videoInfo) => videoInfo.Variants
        .Where(v => v.Bitrate.HasValue)
        .Max(v => v.Bitrate);
}