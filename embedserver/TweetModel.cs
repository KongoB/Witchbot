using System.Text.Json.Serialization;

namespace embedserver
{
    public class FxTwitterResponse
    {
        [JsonPropertyName("tweet")]
        public Tweet? Tweet { get; set; }
    }
    public class Tweet
    {
        [JsonPropertyName("url")]
        public string? Url {get; set; }

        [JsonPropertyName("text")]
        public string? Text {get; set; }

        [JsonPropertyName("author")]
        public Author? Author {get; set; }

        [JsonPropertyName("media")]
        public Media? Media {get; set; }
    }

    public class Author
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("screen_name")]
        public string? ScreenName { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }
    }

    public class Media
    {
        [JsonPropertyName("videos")]
        public List<Video>? Videos { get; set; }
    }

    public class Video
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string? ThumbnailUrl { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("formats")]
        public List<VideoFormat>? Formats { get; set; }
    }

    public class VideoFormat
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("bitrate")]
        public int Bitrate { get; set; }

        [JsonPropertyName("container")]
        public string? Container { get; set; }
    }
}