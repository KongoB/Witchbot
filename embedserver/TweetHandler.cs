using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace embedserver
{

    public class TweetHandler
    {
        public static async Task<IResult> HandleAsync(string username, string tweetId, HttpClient httpClient, MemoryCache cache)
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

                string videoUrl = bestFormat?.Url ?? videos[0].Url!;

                string thumbnailUrl = videos[0].ThumbnailUrl ?? "";
                string authorName = tweetData.Tweet?.Author?.Name ?? "Unknown user";
                string tweetText = tweetData.Tweet?.Text ?? "";
                string avatarUrl = tweetData.Tweet?.Author?.AvatarUrl ?? "";
                string screenName = tweetData.Tweet?.Author?.ScreenName ?? "";
                int width = videos[0].Width;
                int height = videos[0].Height;

                string authorNameEncoded = Uri.EscapeDataString(authorName);
                string screenNameEncoded = Uri.EscapeDataString(screenName);
                string avatarUrlEncoded = Uri.EscapeDataString(avatarUrl);
                
                string html = $"""
                    <html>
                        <head>
                            <meta name="twitter:card" content="player" />
                            <meta name="twitter:card" content="player" />
                            <meta name="twitter:image" content="{thumbnail}" />
                            <meta name="twitter:player:width" content="{width}" />
                            <meta name="twitter:player:height" content="{height}" />
                            <meta name="twitter:player:stream" content="{videoUrl}" />
                            <meta name="twitter:player:stream:content_type" content="video/mp4" />
                            <meta property="og:video" content="{videoUrl}" />
                            <meta property="og:title" content="{authorName}" />
                            <meta property="og:description" content="{tweetText}" />
                            <meta property="og:image" content="{thumbnailUrl}" />
                            <meta property="og:video:width" content="{width}" />
                            <meta property="og:video:height" content="{height}" />
                            <meta property="og:video:type" content="video/mp4" />
                            <meta property="og:site_name" content="x.com" />
                            <meta property="og:type" content="video.other" />
                            <meta property="og:video:secure_url" content="{videoUrl}" />
                            <link rel="alternate" type="application/json+oembed" 
                                href="https://bongobot.online/oembed?authorName={authorNameEncoded}&screenName={screenNameEncoded}&avatarUrl={avatarUrlEncoded}" 
                                title="{authorName}" />
                            <link rel="apple-touch-icon" href="{avatarUrl}"/>
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

            string apiUrl = $"https://api.fxtwitter.com/{username}/status/{tweetId}";
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
                return Results.Json(new {has_video = false});
            }

            var videos = tweetData.Tweet?.Media?.Videos;


            var hasVideo = videos != null && videos.Any();
            return Results.Json(new {has_video = hasVideo});
        }
    }
}