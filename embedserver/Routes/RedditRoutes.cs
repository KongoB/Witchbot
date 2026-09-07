using Microsoft.Extensions.Caching.Memory;

namespace embedserver
{
    public static class RedditRoutes
    {
        public const string TestId = "999001";

        public static void MapRedditRoutes(this WebApplication app, HttpClient httpClient, MemoryCache cache, string baseUrl)
        {
            app.MapGet("/test31", () => Results.Content($$"""
                <html>
                    <head>
                        <meta property="theme-color" content="#FF4500" />
                        <meta name="theme-color" content="#FF4500" />
                        <meta property="og:title" content="r/aww - Test post (OG fallback)" />
                        <meta property="og:description" content="If you can read this, the Mastodon path failed and this is the OG fallback." />
                        <meta property="og:image" content="https://www.redditstatic.com/avatars/defaults/v2/avatar_default_1.png" />
                        <meta property="twitter:image" content="0" />
                        <link rel="alternate" type="application/json+oembed" href="{{baseUrl}}/oembed?text=%E2%AC%86%EF%B8%8F%201.2K%20%20%F0%9F%92%AC%20340&status={{TestId}}&author=testuser" title="r/aww" />
                        <link rel="alternate" type="application/activity+json" href="{{baseUrl}}/users/testuser/statuses/{{TestId}}" />
                        <link rel="icon" sizes="32x32" type="image/png" href="{{baseUrl}}/icon32.png" />
                    </head>
                </html>
                """, "text/html"));
        }

        public static IResult TestStatusJson()
        {
            Console.WriteLine("★ test status JSON served");

            string postUrl = "https://www.reddit.com/r/aww/comments/abc123/";

            return Results.Json(new
            {
                id = TestId,
                url = postUrl,
                uri = postUrl,
                created_at = "2026-08-01T12:00:00.000Z",
                edited_at = (string?)null,
                reblog = (object?)null,
                in_reply_to_id = (string?)null,
                in_reply_to_account_id = (string?)null,
                language = "en",
                content = "First line of a fake Reddit post<br>\u200A\u200A<br>Second paragraph with <b>bold</b>, <i>italic</i> and <a href=\"https://www.reddit.com\">a link</a><br>\u200A\u200A<br><b>\u2B06\uFE0F 1.2K&ensp;\uD83D\uDCAC 340</b>",
                spoiler_text = "",
                visibility = "public",
                application = new { name = "Witchbot", website = (string?)null },
                media_attachments = Array.Empty<object>(),
                account = new
                {
                    id = TestId,
                    display_name = "Fake Post Author",
                    username = "testuser",
                    acct = "testuser",
                    url = postUrl,
                    uri = postUrl,
                    created_at = "2023-01-01T00:00:00.000Z",
                    locked = false,
                    bot = false,
                    discoverable = true,
                    indexable = false,
                    group = false,
                    avatar = "https://www.redditstatic.com/avatars/defaults/v2/avatar_default_1.png",
                    avatar_static = "https://www.redditstatic.com/avatars/defaults/v2/avatar_default_1.png",
                    header = "",
                    header_static = "",
                    followers_count = 0,
                    following_count = 0,
                    statuses_count = 0,
                    hide_collections = false,
                    noindex = false,
                    emojis = Array.Empty<object>(),
                    roles = Array.Empty<object>(),
                    fields = Array.Empty<object>()
                },
                mentions = Array.Empty<object>(),
                tags = Array.Empty<object>(),
                emojis = Array.Empty<object>(),
                card = (object?)null,
                poll = (object?)null
            });
        }
    }
}