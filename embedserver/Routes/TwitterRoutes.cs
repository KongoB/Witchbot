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

            app.MapGet("/gif/{tweetId}.webp", async (string tweetId) =>
            {
                return await TweetHandler.HandleGifAsync(tweetId, httpClient, cache);
            });
        }
    }

}