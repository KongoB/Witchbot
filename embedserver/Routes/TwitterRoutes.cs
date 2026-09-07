using Microsoft.Extensions.Caching.Memory;

namespace embedserver
{
    public static class TwitterRoutes
    {
        public static void MapTwitterRoutes(this WebApplication app, HttpClient httpClient, MemoryCache cache, string baseUrl)
        {
            app.MapGet("/{username}/status/{tweetId}", async (string username, string tweetId) =>
            {
                return await TweetHandler.HandleAsync(username, tweetId, httpClient, cache, baseUrl);
            });

            app.MapGet("/check/{username}/{tweetId}", async (string username, string tweetId) =>
            {
                return await TweetHandler.HasVideo(username, tweetId, httpClient, cache);
            });

            app.MapGet("/oembed", (string text, string status, string author, string? provider) =>
            {

                Console.WriteLine($"oEmbed request received");
                string statusUrl = $"https://x.com/{author}/status/{status}";

                return Results.Json(new
                {
                    author_name = text,
                    author_url = statusUrl,
                    provider_name = string.IsNullOrEmpty(provider) ? "Witch" : provider,
                    provider_url = string.IsNullOrEmpty(provider) ? "https://witchbot.net" : statusUrl,
                    title = "Witch",
                    type = "rich",
                    version = "1.0"
                });
            });

            app.MapGet("/users/{username}/statuses/{tweetId}", async (string username, string tweetId) =>
            {
                if (tweetId == RedditRoutes.TestId) return RedditRoutes.TestStatusJson();
                Console.WriteLine($"Activity JSON request for {tweetId}");
                return await TweetHandler.HandleActivityAsync(username, tweetId, httpClient, cache, baseUrl);
            });

            app.MapGet("/api/v1/statuses/{tweetId}", async (string tweetId) =>
            {
                if (tweetId == RedditRoutes.TestId) return RedditRoutes.TestStatusJson();
                Console.WriteLine($"Mastodon API request for {tweetId}");
                return await TweetHandler.HandleActivityAsync("", tweetId, httpClient, cache, baseUrl);
            });

            app.MapGet("/gif/{tweetId}.webp", async (string tweetId) =>
            {
                return await TweetHandler.HandleGifAsync(tweetId, httpClient, cache);
            });
        }
    }

}