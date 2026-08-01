using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace embedserver
{

    public class TweetHandler
    {
        public static async Task<IResult> HandleAsync(string username, string tweetId, HttpClient httpClient, MemoryCache cache, string baseUrl)
        {
            FxTwitterResponse? tweetData = await FetchTweetAsync(username, tweetId, httpClient, cache);

            if (tweetData == null)
            {
                System.Console.WriteLine("Twitter post data not found");
                return Results.NotFound("not found");
            }

            var videos = tweetData.Tweet?.Media?.Videos;

            //Handle videos
            if (videos != null && videos.Any())
            {
                System.Console.WriteLine($"Video found, servicing embed for {username} and {tweetId}");

                var bestFormat = videos[0].Formats?
                .Where(f => f.Container == "mp4")
                .OrderByDescending(f => f.Bitrate)
                .FirstOrDefault();

                if (videos[0].Type == "gif")
                    _ = HandleGifAsync(tweetId, httpClient, cache);

                string videoUrl = bestFormat?.Url ?? videos[0].Url!;

                string thumbnailUrl = videos[0].ThumbnailUrl ?? "";
                string authorName = tweetData.Tweet?.Author?.Name ?? "Unknown user";
                string tweetText = tweetData.Tweet?.Text ?? "";
                string avatarUrl = tweetData.Tweet?.Author?.AvatarUrl ?? "";
                string screenName = tweetData.Tweet?.Author?.ScreenName ?? "";
                int width = videos[0].Width;
                int height = videos[0].Height;
                int replies = tweetData.Tweet?.Replies ?? 0;
                int retweets = tweetData.Tweet?.Retweets ?? 0;
                int likes = tweetData.Tweet?.Likes ?? 0;
                int views = tweetData.Tweet?.Views ?? 0;

                string statusUrl = $"https://x.com/{screenName}/status/{tweetId}";
                string authorLine = $"{authorName} (@{screenName})";

                var statsParts = new List<string>();
                if (replies > 0) statsParts.Add($"💬 {FormatCount(replies)}");
                if (retweets > 0) statsParts.Add($"🔁 {FormatCount(retweets)}");
                if (likes > 0) statsParts.Add($"❤️ {FormatCount(likes)}");
                if (views > 0) statsParts.Add($"👁️ {FormatCount(views)}");
                string statsText = string.Join("  ", statsParts);

                string oembedText = tweetText.Trim().Length > 0 ? Truncate(tweetText, 255) : statsText;
                string provider = (tweetText.Trim().Length > 0 && statsText.Length > 0) ? statsText : "";

                string oembedUrl = $"{baseUrl}/oembed" +
                     $"?text={Uri.EscapeDataString(oembedText)}" +
                    $"&status={Uri.EscapeDataString(tweetId)}" +
                    $"&author={Uri.EscapeDataString(screenName)}" +
                    (provider.Length > 0 ? $"&provider={Uri.EscapeDataString(provider)}" : "");

                string thumbnailUrlH = System.Net.WebUtility.HtmlEncode(thumbnailUrl);
                string authorNameH = System.Net.WebUtility.HtmlEncode(authorName);
                string avatarUrlH = System.Net.WebUtility.HtmlEncode(avatarUrl);
                string videoUrlH = System.Net.WebUtility.HtmlEncode(videoUrl);
                string oembedUrlH = System.Net.WebUtility.HtmlEncode(oembedUrl);
                string ogTitleH = System.Net.WebUtility.HtmlEncode($"{authorName} (@{screenName})");
                string tweetTextH = System.Net.WebUtility.HtmlEncode(tweetText);


                string html = $"""
                    <html>
                        <head>
                                <meta property="twitter:card" content="player" />
                                <meta property="twitter:player:width" content="{width}" />
                                <meta property="twitter:player:height" content="{height}" />
                                <meta property="twitter:player:stream" content="{videoUrlH}" />
                                <meta property="twitter:player:stream:content_type" content="video/mp4" />
                                <meta property="twitter:image" content="0" />
                                <meta property="og:video" content="{videoUrlH}" />
                                <meta property="og:video:secure_url" content="{videoUrlH}" />
                                <meta property="og:video:width" content="{width}" />
                                <meta property="og:video:height" content="{height}" />
                                <meta property="og:video:type" content="video/mp4" />
                                <meta property="og:title" content="{ogTitleH}" />
                                <meta property="og:description" content="{tweetTextH}" />
                                <meta property="og:site_name" content="Witchbot" />
                                <meta property="og:image" content="{thumbnailUrlH}" />
                                <meta property="og:url" content="https://x.com/{screenName}/status/{tweetId}"/>
                                <link rel="apple-touch-icon" href="{avatarUrlH}" />
                                <link rel="canonical" href="https://x.com/{screenName}/status/{tweetId}"/>
                                <link rel="alternate" type="application/json+oembed" href="{oembedUrlH}" title="{authorNameH}" />
                                <link rel="alternate" type="application/activity+json" href="{baseUrl}/users/{screenName}/statuses/{tweetId}" />
                                <link rel="icon" sizes="32x32" type="image/png" href="{baseUrl}/icon32.png" />
                        </head>
                    </html>
                """;
                return Results.Content(html, "text/html");
            }
            System.Console.WriteLine($"No video found for {username} and {tweetId}");

            return Results.Ok();
        }

        private static async Task<FxTwitterResponse?> FetchTweetAsync(string username, string tweetId, HttpClient httpClient, MemoryCache cache)
        {
            Console.WriteLine($"Request for tweet {tweetId}");

            if (cache.TryGetValue(tweetId, out FxTwitterResponse? cached))
            {
                System.Console.WriteLine($"Cache hit for {tweetId}");
                return cached;
            }

            string apiUrl = string.IsNullOrEmpty(username)
                ? $"https://api.fxtwitter.com/status/{tweetId}"
                : $"https://api.fxtwitter.com/{username}/status/{tweetId}";

            Console.WriteLine($"Received request for {apiUrl}");
            var response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Post not found. {response.StatusCode}. URL: {apiUrl}");
                return null;
            }


            string json = await response.Content.ReadAsStringAsync();
            var tweetData = JsonSerializer.Deserialize<FxTwitterResponse>(json);

            if (tweetData?.Tweet == null)
            {
                Console.WriteLine("Could not parse post data");
                return null;
            }

            cache.Set(tweetId, tweetData, TimeSpan.FromMinutes(5));
            System.Console.WriteLine($"Cached {tweetId}");

            return tweetData;
        }

        public static async Task<IResult> HasVideo(string username, string tweetId, HttpClient httpClient, MemoryCache cache)
        {
            FxTwitterResponse? tweetData = await FetchTweetAsync(username, tweetId, httpClient, cache);

            if (tweetData == null)
            {
                System.Console.WriteLine("Twitter post data not found");
                return Results.Json(new { has_video = false });
            }

            var videos = tweetData.Tweet?.Media?.Videos;


            var hasVideo = videos != null && videos.Any();
            return Results.Json(new { has_video = hasVideo });
        }

        public static async Task<IResult> HandleActivityAsync(string username, string tweetId, HttpClient httpClient, MemoryCache cache, string baseUrl)
        {
            FxTwitterResponse? tweetData = await FetchTweetAsync(username, tweetId, httpClient, cache);
            if (tweetData?.Tweet == null) return Results.NotFound("not found");

            var tweet = tweetData.Tweet;
            var video = tweet.Media?.Videos?.FirstOrDefault();
            if (video == null) return Results.NotFound("no video");

            var bestFormat = video.Formats?
                .Where(f => f.Container == "mp4")
                .OrderByDescending(f => f.Bitrate)
                .FirstOrDefault();
            string videoUrl = bestFormat?.Url ?? video.Url!;

            string screenName = tweet.Author?.ScreenName ?? "";
            string statusUrl = $"https://x.com/{screenName}/status/{tweetId}";

            var statsParts = new List<string>();
            if (tweet.Replies > 0) statsParts.Add($"💬 {FormatCount(tweet.Replies)}");
            if (tweet.Retweets > 0) statsParts.Add($"🔁 {FormatCount(tweet.Retweets)}");
            if (tweet.Likes > 0) statsParts.Add($"❤️ {FormatCount(tweet.Likes)}");
            if (tweet.Views > 0) statsParts.Add($"👁️ {FormatCount(tweet.Views)}");
            string statsText = string.Join("   ", statsParts);

            string contentHtml = System.Net.WebUtility.HtmlEncode(tweet.Text ?? "").Replace("\n", "<br>");

            var quote = tweet.Quote;
            if (quote != null)
            {
                string qName = System.Net.WebUtility.HtmlEncode(quote.Author?.Name ?? "Unknown");
                string qHandle = quote.Author?.ScreenName ?? "";
                string qText = System.Net.WebUtility.HtmlEncode(quote.Text ?? "").Replace("\n", "<br>");
                string qUrl = quote.Url ?? $"https://x.com/{qHandle}";

                contentHtml +=
                    $"<br><br><blockquote>" +
                    $"<b><a href=\"{qUrl}\">Quoting</a> {qName} " +
                    $"(<a href=\"https://x.com/{qHandle}\">@{qHandle}</a>)</b>" +
                    $"<br><br>{qText}</blockquote>";
            }

            if (statsText.Length > 0)
                contentHtml += (contentHtml.Length > 0 ? "<br><br>" : "") + statsText;

            string createdAt = DateTimeOffset.FromUnixTimeSeconds(tweet.CreatedTimestamp)
                .UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

            bool isGif = video.Type == "gif";

            object attachment;

            if (isGif)
            {
                attachment = new
                {
                    id = "1",
                    type = "image",
                    url = $"{baseUrl}/gif/{tweetId}.webp",
                    preview_url = (string?)video.ThumbnailUrl,
                    remote_url = (string?)null,
                    description = (string?)null,
                    meta = new
                    {
                        original = new
                        {
                            width = video.Width,
                            height = video.Height
                        }
                    }
                };
            }
            else
            {
                attachment = new
                {
                    id = "1",
                    type = "video",
                    url = videoUrl,
                    preview_url = (string?)video.ThumbnailUrl,
                    remote_url = (string?)null,
                    preview_remote_url = (string?)null,
                    text_url = (string?)null,
                    description = (string?)null,
                    meta = new
                    {
                        original = new
                        {
                            width = video.Width,
                            height = video.Height,
                            size = $"{video.Width}x{video.Height}",
                            aspect = (double)video.Width / video.Height
                        }
                    }
                };
            }

            var response = new
            {
                id = tweetId,
                url = statusUrl,
                uri = statusUrl,
                created_at = createdAt,
                edited_at = (string?)null,
                reblog = (object?)null,
                in_reply_to_id = (string?)null,
                in_reply_to_account_id = (string?)null,
                language = "en",
                content = contentHtml,
                spoiler_text = "",
                visibility = "public",
                application = new { name = "Witch", website = (string?)null },
                account = new
                {
                    id = tweetId,
                    display_name = tweet.Author?.Name ?? "",
                    username = screenName,
                    acct = screenName,
                    url = statusUrl,
                    uri = statusUrl,
                    created_at = createdAt,
                    locked = false,
                    bot = false,
                    discoverable = true,
                    group = false,
                    avatar = tweet.Author?.AvatarUrl,
                    avatar_static = tweet.Author?.AvatarUrl,
                    emojis = Array.Empty<object>(),
                    fields = Array.Empty<object>()
                },
                media_attachments = new object[] { attachment },
                mentions = Array.Empty<object>(),
                tags = Array.Empty<object>(),
                emojis = Array.Empty<object>(),
                card = (object?)null,
                poll = (object?)null
            };
            return Results.Json(response);
        }
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Task<bool>> InFlight = new();

        public static async Task<IResult> HandleGifAsync(string tweetId, HttpClient httpClient, MemoryCache cache)
        {
            string dir = Path.Combine(Path.GetTempPath(), "twittergifs");
            Directory.CreateDirectory(dir);
            string webpPath = Path.Combine(dir, $"{tweetId}.webp");

            if (!File.Exists(webpPath))
            {
                var task = InFlight.GetOrAdd(tweetId,
                    id => TranscodeAsync(id, httpClient, cache, dir, webpPath));

                bool ok = await task;
                InFlight.TryRemove(tweetId, out _);

                if (!ok) return Results.StatusCode(500);
            }

            return Results.File(webpPath, "image/webp");
        }

        private static async Task<bool> TranscodeAsync(string tweetId, HttpClient httpClient, MemoryCache cache, string dir, string webpPath)
        {
            var tweetData = await FetchTweetAsync("", tweetId, httpClient, cache);
            var video = tweetData?.Tweet?.Media?.Videos?.FirstOrDefault();
            if (video?.Url == null) return false;

            Console.WriteLine($"Transcoding gif for {tweetId}");

            string mp4Path = Path.Combine(dir, $"{tweetId}.mp4");
            string tempPath = Path.Combine(dir, $"{tweetId}.tmp.webp");   // <- work-in-progress name

            var bytes = await httpClient.GetByteArrayAsync(video.Url);
            await File.WriteAllBytesAsync(mp4Path, bytes);

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-y -i \"{mp4Path}\" -c:v libwebp -q:v 75 -loop 0 \"{tempPath}\"",
                RedirectStandardError = true
            };
            using var proc = System.Diagnostics.Process.Start(psi)!;
            await proc.WaitForExitAsync();
            File.Delete(mp4Path);

            if (proc.ExitCode != 0 || !File.Exists(tempPath))
            {
                Console.WriteLine($"ffmpeg failed for {tweetId}");
                return false;
            }

            File.Move(tempPath, webpPath, overwrite: true);   // <- the atomic "it's ready" moment
            Console.WriteLine($"Transcode complete for {tweetId}");
            return true;
        }

        private static string FormatCount(int n) =>
            n >= 1_000_000 ? (n / 1_000_000.0).ToString("0.#") + "M"
            : n >= 1_000 ? (n / 1_000.0).ToString("0.#") + "K"
            : n.ToString();

        private static string Truncate(string s, int max) =>
             s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }
}