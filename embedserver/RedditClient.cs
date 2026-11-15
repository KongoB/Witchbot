using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace embedserver
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RedditPostType { Text, Image, Gallery, Video, Link, Removed, Unknown }

    public class RedditImage
    {
        public string Url { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Animated { get; set; }
    }

    public class RedditVideo
    {
        public string FallbackUrl { get; set; } = "";
        public string? HlsUrl { get; set; }
        public string? DashUrl { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Duration { get; set; }
        public bool HasAudio { get; set; }
        public bool IsGif { get; set; }
    }

    public class RedditPost
    {
        public string Id { get; set; } = "";
        public RedditPostType Type { get; set; }
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public string Author { get; set; } = "";
        public string Subreddit { get; set; } = "";          // "r/aww"
        public string Permalink { get; set; } = "";          // full https URL
        public string? LinkUrl { get; set; }                 // outbound URL for link posts
        public string? ThumbnailUrl { get; set; }
        public string? CrosspostFrom { get; set; }           // "r/original" when this is a crosspost
        public int Upvotes { get; set; }
        public int Comments { get; set; }
        public long CreatedUtc { get; set; }
        public bool Nsfw { get; set; }
        public bool Spoiler { get; set; }
        public List<RedditImage> Images { get; set; } = new();
        public RedditVideo? Video { get; set; }
    }

    public class RedditClient
    {
        private static readonly Regex IdPattern = new("^[0-9a-z]{1,12}$", RegexOptions.Compiled);

        private readonly HttpClient _http;
        private readonly MemoryCache _cache;

        public bool IsConfigured { get; }

        public RedditClient(string? session, string? userAgent, MemoryCache cache)
        {
            _cache = cache;
            IsConfigured = !string.IsNullOrWhiteSpace(session) && !string.IsNullOrWhiteSpace(userAgent);

            // UseCookies = false makes HttpClient send our Cookie header exactly as written
            // instead of managing cookies itself.
            var handler = new SocketsHttpHandler
            {
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.All,
                PooledConnectionLifetime = TimeSpan.FromMinutes(10)
            };
            _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            if (IsConfigured)
            {
                string cookie = session!.Trim();
                if (!cookie.StartsWith("reddit_session=")) cookie = "reddit_session=" + cookie;

                _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent!.Trim());
                _http.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookie);
                Console.WriteLine($"Reddit: session loaded ({cookie.Length} chars), user agent loaded");
            }
            else
            {
                Console.WriteLine("Reddit: WARNING - Reddit:Session or Reddit:UserAgent missing, Reddit embeds disabled");
            }
        }

        /// <summary>Returns the post, or null when it can't be fetched (never throws).</summary>
        public async Task<RedditPost?> GetPostAsync(string id)
        {
            id = id.Trim().ToLowerInvariant();
            if (!IsConfigured || !IdPattern.IsMatch(id)) return null;

            string key = $"reddit:{id}";
            if (_cache.TryGetValue(key, out RedditPost? cached)) return cached;

            RedditPost? post = null;
            try
            {
                post = await FetchAsync(id);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Reddit: fetch failed for {id}: {e.GetType().Name}: {e.Message}");
            }

            // Failures are cached too (briefly) so a broken link isn't retried on every request.
            _cache.Set(key, post, post != null ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(60));
            return post;
        }

        private async Task<RedditPost?> FetchAsync(string id)
        {
            string url = $"https://www.reddit.com/comments/{id}.json?raw_json=1";
            using var response = await _http.GetAsync(url);

            string contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!response.IsSuccessStatusCode || !contentType.Contains("json"))
            {
                Console.WriteLine($"Reddit: {(int)response.StatusCode} {contentType} for {id}" +
                    (response.StatusCode == HttpStatusCode.Forbidden ? " (cookie expired or request blocked?)" : ""));
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            // Shape: [ { data: { children: [ { data: POST } ] } }, { ...comments... } ]
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0) return null;
            if (!TryGet(root[0], "data", out var listing) || !TryGet(listing, "children", out var children)) return null;
            if (children.ValueKind != JsonValueKind.Array || children.GetArrayLength() == 0) return null;
            if (!TryGet(children[0], "data", out var data)) return null;

            return Parse(id, data);
        }

        private static RedditPost Parse(string id, JsonElement data)
        {
            var post = new RedditPost
            {
                Id = id,
                Title = Str(data, "title").Trim(),
                Text = Str(data, "selftext").Trim(),
                Author = Str(data, "author"),
                Subreddit = Str(data, "subreddit_name_prefixed"),
                Permalink = "https://www.reddit.com" + Str(data, "permalink"),
                Upvotes = Int(data, "ups"),
                Comments = Int(data, "num_comments"),
                CreatedUtc = (long)Num(data, "created_utc"),
                Nsfw = Bool(data, "over_18"),
                Spoiler = Bool(data, "spoiler")
            };

            // A crosspost keeps its own title/author, but the media lives on the original post.
            JsonElement media = data;
            if (TryGet(data, "crosspost_parent_list", out var parents) &&
                parents.ValueKind == JsonValueKind.Array && parents.GetArrayLength() > 0)
            {
                media = parents[0];
                post.CrosspostFrom = Str(media, "subreddit_name_prefixed");
                if (post.Text.Length == 0) post.Text = Str(media, "selftext").Trim();
            }

            // Preview image (used as video thumbnail / link thumbnail / image dimensions)
            int previewW = 0, previewH = 0;
            if (TryGet(media, "preview", out var preview) && TryGet(preview, "images", out var pImages) &&
                pImages.ValueKind == JsonValueKind.Array && pImages.GetArrayLength() > 0 &&
                TryGet(pImages[0], "source", out var source))
            {
                post.ThumbnailUrl = NullIfEmpty(Str(source, "url"));
                previewW = Int(source, "width");
                previewH = Int(source, "height");
            }

            string hint = Str(media, "post_hint");
            string url = Str(media, "url");
            bool hasGallery = TryGet(media, "gallery_data", out var gallery) && gallery.ValueKind == JsonValueKind.Object;
            bool hasVideo = TryGetVideo(media, out var redditVideo);
            bool removed = Str(data, "removed_by_category").Length > 0;

            if (removed) post.Type = RedditPostType.Removed;
            else if (hint == "hosted:video" || (hasVideo && Bool(media, "is_video"))) post.Type = RedditPostType.Video;
            else if (hasGallery) post.Type = RedditPostType.Gallery;
            else if (hint == "image" || url.StartsWith("https://i.redd.it/")) post.Type = RedditPostType.Image;
            else if (hint == "self") post.Type = RedditPostType.Text;
            else if (hint == "link" || hint == "rich:video") post.Type = RedditPostType.Link;
            else if (hint.Length == 0)
            {
                bool ownUrl = url.Length == 0 || url.StartsWith("/") || url.StartsWith("https://www.reddit.com/");
                post.Type = ownUrl ? RedditPostType.Text : RedditPostType.Link;
            }
            else post.Type = RedditPostType.Unknown;

            switch (post.Type)
            {
                case RedditPostType.Video when hasVideo:
                    post.Video = new RedditVideo
                    {
                        FallbackUrl = Str(redditVideo, "fallback_url"),
                        HlsUrl = NullIfEmpty(Str(redditVideo, "hls_url")),
                        DashUrl = NullIfEmpty(Str(redditVideo, "dash_url")),
                        Width = Int(redditVideo, "width"),
                        Height = Int(redditVideo, "height"),
                        Duration = Int(redditVideo, "duration"),
                        IsGif = Bool(redditVideo, "is_gif"),
                        // has_audio is absent on older posts; assume audio unless told otherwise
                        HasAudio = !TryGet(redditVideo, "has_audio", out var ha) || ha.ValueKind != JsonValueKind.False
                    };
                    break;

                case RedditPostType.Image:
                    post.Images.Add(new RedditImage
                    {
                        Url = url,
                        Width = previewW,
                        Height = previewH,
                        Animated = url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
                    });
                    break;

                case RedditPostType.Gallery:
                    ParseGallery(media, gallery, post.Images);
                    break;

                case RedditPostType.Link:
                    post.LinkUrl = url;
                    if (post.ThumbnailUrl == null)
                    {
                        string thumb = Str(media, "thumbnail");
                        if (thumb.StartsWith("https://")) post.ThumbnailUrl = thumb;
                    }
                    break;
            }

            return post;
        }

        private static void ParseGallery(JsonElement media, JsonElement gallery, List<RedditImage> images)
        {
            if (!TryGet(gallery, "items", out var items) || items.ValueKind != JsonValueKind.Array) return;
            if (!TryGet(media, "media_metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object) return;

            foreach (var item in items.EnumerateArray())
            {
                string mediaId = Str(item, "media_id");
                if (mediaId.Length == 0 || !TryGet(metadata, mediaId, out var meta)) continue;
                if (Str(meta, "status") != "valid" || !TryGet(meta, "s", out var s)) continue;

                string kind = Str(meta, "e");
                string imageUrl;
                bool animated = false;

                if (kind == "Image")
                {
                    imageUrl = Str(s, "u").Replace("https://preview.redd.it/", "https://i.redd.it/").Split('?')[0];
                }
                else if (kind == "AnimatedImage")
                {
                    imageUrl = Str(s, "gif");
                    animated = true;
                }
                else continue;

                if (imageUrl.Length == 0) continue;
                images.Add(new RedditImage { Url = imageUrl, Width = Int(s, "x"), Height = Int(s, "y"), Animated = animated });
            }
        }

        private static bool TryGetVideo(JsonElement media, out JsonElement redditVideo)
        {
            redditVideo = default;
            foreach (string name in new[] { "secure_media", "media" })
            {
                if (TryGet(media, name, out var m) && m.ValueKind == JsonValueKind.Object &&
                    TryGet(m, "reddit_video", out redditVideo) && redditVideo.ValueKind == JsonValueKind.Object)
                    return true;
            }
            return false;
        }


        private static bool TryGet(JsonElement el, string name, out JsonElement value)
        {
            value = default;
            return el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out value) &&
                   value.ValueKind != JsonValueKind.Null;
        }

        private static string Str(JsonElement el, string name) =>
            TryGet(el, name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

        private static double Num(JsonElement el, string name) =>
            TryGet(el, name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

        private static int Int(JsonElement el, string name) => (int)Num(el, name);

        private static bool Bool(JsonElement el, string name) =>
            TryGet(el, name, out var v) && v.ValueKind == JsonValueKind.True;

        private static string? NullIfEmpty(string s) => s.Length == 0 ? null : s;
    }
}
