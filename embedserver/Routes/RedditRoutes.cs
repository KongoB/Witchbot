namespace embedserver
{
    public static class RedditRoutes
    {
        public static void MapRedditRoutes(this WebApplication app, RedditClient reddit, string baseUrl)
        {
            // Temporary: shows how a post was fetched and classified. Remove before going live.
            app.MapGet("/reddit/debug/{id}", async (string id) =>
            {
                RedditPost? post = await reddit.GetPostAsync(id);
                return post == null ? Results.NotFound("could not fetch post (see server log)") : Results.Json(post);
            });

            // Landing pages. The paths copy Reddit's own, so swapping reddit.com for our domain just works.
            // {*rest} swallows whatever follows the ID (the title slug, a trailing slash, or nothing).
            app.MapGet("/r/{subreddit}/comments/{id}/{*rest}", async (string id) =>
            {
                return await RedditHandler.LandingAsync(id, reddit, baseUrl);
            });

            // Share links from the Reddit app: /r/sub/s/CODE. Find the real post first, then behave as above.
            app.MapGet("/r/{subreddit}/s/{code}", async (string subreddit, string code) =>
            {
                string? id = await reddit.ResolveShareLinkAsync(subreddit, code);
                if (id == null)
                {
                    return Results.Redirect($"https://www.reddit.com/r/{Uri.EscapeDataString(subreddit)}/s/{Uri.EscapeDataString(code)}");
                }

                return await RedditHandler.LandingAsync(id, reddit, baseUrl);
            });

            app.MapGet("/user/{name}/comments/{id}/{*rest}", async (string id) =>
            {
                return await RedditHandler.LandingAsync(id, reddit, baseUrl);
            });

            app.MapGet("/comments/{id}/{*rest}", async (string id) =>
            {
                return await RedditHandler.LandingAsync(id, reddit, baseUrl);
            });
        }
    }
}
