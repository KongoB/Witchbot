using Microsoft.Extensions.Caching.Memory;

namespace embedserver
{
    /// <summary>
    /// Routes Discord calls on its own once a landing page says "I'm a Mastodon post".
    /// Every site shares them, so each one looks at the ID and passes the request to the right site's code.
    /// </summary>
    public static class MastodonRoutes
    {
        public static void MapMastodonRoutes(this WebApplication app, HttpClient httpClient, MemoryCache cache, RedditClient reddit, string baseUrl)
        {
            app.MapGet("/users/{username}/statuses/{id}", async (string username, string id) =>
            {
                return await StatusAsync(username, id, httpClient, cache, reddit, baseUrl);
            });

            app.MapGet("/api/v1/statuses/{id}", async (string id) =>
            {
                return await StatusAsync("", id, httpClient, cache, reddit, baseUrl);
            });

            // Used by the plain (fallback) embed: "text" is the small line at the top, "provider" the footer.
            // Twitter pages pass status + author, Reddit pages pass url.
            app.MapGet("/oembed", (string text, string? status, string? author, string? provider, string? url) =>
            {
                bool isReddit = url != null && url.StartsWith("https://www.reddit.com/");
                string statusUrl = isReddit ? url! : $"https://x.com/{author}/status/{status}";

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
        }

        private static async Task<IResult> StatusAsync(string username, string id, HttpClient httpClient, MemoryCache cache, RedditClient reddit, string baseUrl)
        {
            if (RedditId.TryToPostId(id, out string postId))
            {
                Console.WriteLine($"Status request for Reddit post {postId}");
                return await RedditHandler.StatusAsync(postId, reddit, baseUrl);
            }

            Console.WriteLine($"Status request for tweet {id}");
            return await TweetHandler.HandleActivityAsync(username, id, httpClient, cache, baseUrl);
        }
    }
}
