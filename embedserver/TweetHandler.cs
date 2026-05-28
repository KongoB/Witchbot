using System.Text.Json;

namespace embedserver
{

    public class TweetHandler
    {
        public static async Task<IResult> HandleAsync(string url, HttpClient httpClient)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
            url, @"(?:x|twitter)\.com/([^/]+)/status/(\d+)"
        );

            if (!match.Success)
            {
                return Results.BadRequest("Invalid url");
            }

            string username = match.Groups[1].Value;
            string tweetId = match.Groups[2].Value;

            string apiUrl = $"https://api.fxtwitter.com/{username}/status/{tweetId}";
            var response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                return Results.NotFound($"Post not found. {response.StatusCode}. URL: {apiUrl}");
            }


            string json = await response.Content.ReadAsStringAsync();
            var tweetData = JsonSerializer.Deserialize<FxTwitterResponse>(json);

            if (tweetData?.Tweet == null)
            {
                return Results.NotFound("Could not parse post data");
            }

            var videos = tweetData.Tweet.Media?.Videos;


            //Handle videos
            if (videos != null && videos.Any())
            {
                var bestFormat = videos[0].Formats?
                .Where(f => f.Container == "mp4")
                .OrderByDescending(f => f.Bitrate)
                .FirstOrDefault();

                string videoUrl = bestFormat?.Url ?? videos[0].Url!;

                string thumbnailUrl = videos[0].ThumbnailUrl ?? "";
                string authorName = tweetData.Tweet.Author?.Name ?? "Unknown user";
                string tweetText = tweetData.Tweet.Text ?? "";
                string avatarUrl = tweetData.Tweet.Author?.AvatarUrl ?? "";
                string screenName = tweetData.Tweet.Author?.ScreenName ?? "";
                int width = videos[0].Width;
                int height = videos[0].Height;

                string html = $"""
                    <html>
                        <head>
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
                                href="https://localhost:5031/oembed?authorName={authorName}&screenName={screenName}&avatarUrl={avatarUrl}" 
                                title="{authorName}" />
                        </head>
                    </html>
                """;
            }

            return Results.Ok();
        }
    }

}