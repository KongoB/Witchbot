namespace embedserver
{
    public static class RedditRoutes
    {
        public static void MapRedditRoutes(this WebApplication app, RedditClient reddit, RedditVideoService videos, string baseUrl)
        {
            // Temporary: shows how a post was fetched and classified. Remove before going live.
            app.MapGet("/reddit/debug/{id}", async (string id) =>
            {
                RedditPost? post = await reddit.GetPostAsync(id);
                return post == null ? Results.NotFound("could not fetch post (see server log)") : Results.Json(post);
            });

            // The merged video file. If it isn't on disk (first request, or it was cleaned up), this waits
            // for the merge. If the merge fails, it sends the viewer to Reddit's silent file instead.
            app.MapGet("/reddit/video/{id}.mp4", async (string id) =>
            {
                RedditPost? post = await reddit.GetPostAsync(id);
                if (post?.Video == null) return Results.NotFound("no video");

                string? path = await videos.GetOrCreateAsync(post);
                if (path == null) return Results.Redirect(post.Video.FallbackUrl);

                // enableRangeProcessing lets the player ask for parts of the file, which is how seeking works.
                return Results.File(path, "video/mp4", enableRangeProcessing: true);
            });

            // Landing pages. The paths copy Reddit's own, so swapping reddit.com for our domain just works.
            // {*rest} swallows whatever follows the ID (the title slug, a trailing slash, or nothing).
            app.MapGet("/r/{subreddit}/comments/{id}/{*rest}", async (string id) =>
            {
                return await RedditHandler.LandingAsync(id, reddit, videos, baseUrl);
            });

            // Share links from the Reddit app: /r/sub/s/CODE. Find the real post first, then behave as above.
            app.MapGet("/r/{subreddit}/s/{code}", async (string subreddit, string code) =>
            {
                string? id = await reddit.ResolveShareLinkAsync(subreddit, code);
                if (id == null)
                {
                    return Results.Redirect($"https://www.reddit.com/r/{Uri.EscapeDataString(subreddit)}/s/{Uri.EscapeDataString(code)}");
                }

                return await RedditHandler.LandingAsync(id, reddit, videos, baseUrl);
            });

            app.MapGet("/user/{name}/comments/{id}/{*rest}", async (string id) =>
            {
                return await RedditHandler.LandingAsync(id, reddit, videos, baseUrl);
            });

            app.MapGet("/comments/{id}/{*rest}", async (string id) =>
            {
                return await RedditHandler.LandingAsync(id, reddit, videos, baseUrl);
            });
        }
    }
}
