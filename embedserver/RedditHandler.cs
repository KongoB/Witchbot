using System.Net;
using System.Text.RegularExpressions;

namespace embedserver
{
    public static class RedditHandler
    {
        // Discord collapses "<br><br>"; the invisible hair spaces ( ) keep the blank line.
        private const string LineBreak = "<br>  ";
        private const string BlankLine = "<br>  <br>";
        private const string DefaultAvatar = "https://www.redditstatic.com/avatars/defaults/v2/avatar_default_1.png";
        private const int MaxBodyLength = 1000;
        private const int MaxImages = 10;

        /// <summary>
        /// The page Discord's crawler fetches first. It carries plain OG tags (the fallback embed)
        /// and the activity+json link that switches Discord to the Mastodon-style embed.
        /// </summary>
        public static async Task<IResult> LandingAsync(string postId, RedditClient reddit, string baseUrl)
        {
            RedditPost? post = await reddit.GetPostAsync(postId);
            string? statusId = post == null ? null : RedditId.ToStatusId(post.Id);

            if (post == null || statusId == null)
            {
                // Fail open: send everyone (Discord included) to the real post, so Reddit's own embed shows.
                Console.WriteLine($"Reddit: no data for {postId}, redirecting to reddit.com");
                return Results.Redirect($"https://www.reddit.com/comments/{Uri.EscapeDataString(postId)}");
            }

            string description = post.Text.Length > 0 ? Truncate(post.Text, 200) : StatsText(post);
            string image = post.Images.Count > 0 ? post.Images[0].Url : post.ThumbnailUrl ?? DefaultAvatar;

            string oembedUrl = $"{baseUrl}/oembed" +
                $"?text={Uri.EscapeDataString(StatsText(post))}" +
                $"&url={Uri.EscapeDataString(post.Permalink)}" +
                $"&provider=Reddit";
            string statusUrl = $"{baseUrl}/users/{Uri.EscapeDataString(post.Author)}/statuses/{statusId}";

            string html = $"""
                <html>
                    <head>
                        <meta property="theme-color" content="#FF4500" />
                        <meta name="theme-color" content="#FF4500" />
                        <meta property="og:title" content="{H(post.Title)}" />
                        <meta property="og:description" content="{H(description)}" />
                        <meta property="og:site_name" content="{H("Reddit")}" />
                        <meta property="og:image" content="{H(image)}" />
                        <meta property="twitter:image" content="0" />
                        <meta property="og:url" content="{H(post.Permalink)}" />
                        <meta http-equiv="refresh" content="0;url={H(post.Permalink)}" />
                        <link rel="canonical" href="{H(post.Permalink)}" />
                        <link rel="alternate" type="application/json+oembed" href="{H(oembedUrl)}" title="{H(post.Subreddit)}" />
                        <link rel="alternate" type="application/activity+json" href="{H(statusUrl)}" />
                        <link rel="icon" sizes="32x32" type="image/png" href="{baseUrl}/icon32.png" />
                    </head>
                </html>
                """;

            return Results.Content(html, "text/html");
        }

        /// <summary>The Mastodon-style status JSON Discord asks for after reading the landing page.</summary>
        public static async Task<IResult> StatusAsync(string postId, RedditClient reddit)
        {
            RedditPost? post = await reddit.GetPostAsync(postId);
            string? statusId = post == null ? null : RedditId.ToStatusId(post.Id);
            if (post == null || statusId == null) return Results.NotFound("not found");

            string createdAt = DateTimeOffset.FromUnixTimeSeconds(post.CreatedUtc)
                .UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

            // Without "www." so the header reads "@r/sub@reddit.com"; reddit.com redirects to www anyway.
            string accountUrl = post.Permalink.Replace("https://www.reddit.com", "https://reddit.com");

            return Results.Json(new
            {
                id = statusId,
                url = post.Permalink,
                uri = post.Permalink,
                created_at = createdAt,
                edited_at = (string?)null,
                reblog = (object?)null,
                in_reply_to_id = (string?)null,
                in_reply_to_account_id = (string?)null,
                language = "en",
                content = BuildContent(post),
                spoiler_text = "",
                visibility = "public",
                application = new { name = "Witchbot", website = (string?)null },
                media_attachments = BuildMedia(post),
                account = new
                {
                    id = statusId,
                    display_name = $"u/{post.Author}",
                    username = post.Subreddit + " ",
                    acct = post.Subreddit + " ",
                    url = accountUrl,
                    uri = accountUrl,
                    created_at = createdAt,
                    locked = false,
                    bot = false,
                    discoverable = true,
                    indexable = false,
                    group = false,
                    avatar = DefaultAvatar,
                    avatar_static = DefaultAvatar,
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

        /// <summary>Title in bold, then whatever applies to this post, then the stats line.</summary>
        private static string BuildContent(RedditPost post)
        {
            var parts = new List<string> { $"<b>{H(post.Title)}</b>" };

            if (post.CrosspostFrom != null)
                parts.Add($"<i>Crossposted from {H(post.CrosspostFrom)}</i>");

            if (post.Type == RedditPostType.Removed)
                parts.Add("<i>This post was removed.</i>");

            if (post.LinkUrl != null)
                parts.Add($"<a href=\"{H(post.LinkUrl)}\">{H(Truncate(post.LinkUrl, 80))}</a>");

            string body = FormatBody(post.Text);
            if (body.Length > 0) parts.Add(body);

            parts.Add($"<b>{H(StatsText(post)).Replace("  ", "&ensp;")}</b>");

            return string.Join(BlankLine, parts);
        }

        /// <summary>
        /// The pictures or video under the text. Videos use Reddit's own file for now, which has no sound;
        /// the merged version with audio comes in the next step.
        /// </summary>
        private static List<object> BuildMedia(RedditPost post)
        {
            var media = new List<object>();

            if (post.Video != null && post.Video.FallbackUrl.Length > 0)
            {
                media.Add(Attachment("1", "video", post.Video.FallbackUrl, post.ThumbnailUrl, post.Video.Width, post.Video.Height));
                return media;
            }

            int number = 0;
            foreach (RedditImage image in post.Images.Take(MaxImages))
            {
                number++;
                media.Add(Attachment(number.ToString(), "image", image.Url, image.Url, image.Width, image.Height));
            }

            return media;
        }

        private static object Attachment(string id, string type, string url, string? previewUrl, int width, int height)
        {
            if (width <= 0 || height <= 0) { width = 1280; height = 720; }

            // Discord refuses to show media declared larger than 1920px on a side, so declare it at half size.
            if (width > 1920 || height > 1920) { width /= 2; height /= 2; }

            return new
            {
                id,
                type,
                url,
                preview_url = previewUrl,
                remote_url = (string?)null,
                preview_remote_url = (string?)null,
                text_url = (string?)null,
                description = (string?)null,
                meta = new
                {
                    original = new
                    {
                        width,
                        height,
                        size = $"{width}x{height}",
                        aspect = (double)width / height
                    }
                }
            };
        }

        private static string FormatBody(string text)
        {
            string t = text.Replace("\r\n", "\n").Replace("&#x200B;", "").Trim();
            if (t.Length == 0) return "";

            t = Regex.Replace(t, "\n{3,}", "\n\n");
            t = Truncate(t, MaxBodyLength);

            // Encode first so nothing in the post can inject HTML. Everything after this only adds our own tags.
            t = H(t);

            // Reddit posts are written in Markdown. Convert the common parts to the HTML Discord understands.
            t = Regex.Replace(t, @"\\([_*\[\]()~#.!+-])", "$1");                                 // \_  ->  _
            t = Regex.Replace(t, @"\[([^\]\n]+)\]\((https?://[^\s)]+)\)", "<a href=\"$2\">$1</a>");   // [text](url)
            t = Regex.Replace(t, @"(?<![""'>=/\w])https?://[^\s<]+", m => $"<a href=\"{m.Value}\">{m.Value}</a>"); // bare url
            t = Regex.Replace(t, @"\*\*(?!\s)(.+?)(?<!\s)\*\*", "<b>$1</b>");                         // **bold**
            t = Regex.Replace(t, @"(?<![*\w])\*(?![\s*])(.+?)(?<![\s*])\*(?![*\w])", "<i>$1</i>");     // *italic*

            return t.Replace("\n", LineBreak);
        }

        private static string StatsText(RedditPost post)
        {
            string stats = $"⬆️ {FormatCount(post.Upvotes)}";
            if (post.Comments > 0) stats += $"  💬 {FormatCount(post.Comments)}";
            return stats;
        }

        private static string H(string s) => WebUtility.HtmlEncode(s);

        private static string FormatCount(int n) =>
            n >= 1_000_000 ? (n / 1_000_000.0).ToString("0.#") + "M"
            : n >= 1_000 ? (n / 1_000.0).ToString("0.#") + "K"
            : n.ToString();

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }
}
