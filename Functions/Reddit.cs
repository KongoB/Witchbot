using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace vxtwitterbot.Functions
{
    /// <summary>What the bot should post for a message containing Reddit links.</summary>
    public class RedditFix
    {
        public string Content { get; set; } = "";
        public List<string> VideoIds { get; set; } = new();   // posts whose video still has to be merged
    }

    /// <summary>The embedserver's answer to "what is this Reddit post?"</summary>
    public class RedditInfo
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("nsfw")]
        public bool Nsfw { get; set; }

        [JsonPropertyName("needs_merge")]
        public bool NeedsMerge { get; set; }
    }

    public class Reddit
    {
        // Group 1: the whole path, reused as-is on our own domain.
        // Group 2: post ID (normal links).   Groups 3 + 4: subreddit and code (share links).
        private static readonly Regex LinkPattern = new(
            @"https?://(?:www\.|new\.|old\.)?reddit\.com(/(?:r|u|user)/[\w-]+/comments/(\w+)(?:/[^\s?#|<>()]*)?|/r/([\w-]+)/s/(\w+))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Finds the Reddit links in a message and asks the embedserver about each one.
        /// Returns null when there is nothing to fix (no links, or the embedserver couldn't read any of them),
        /// in which case the bot stays silent and the original message keeps Reddit's own embed.
        /// </summary>
        public static async Task<RedditFix?> BuildFixAsync(string message, HttpClient httpClient, string baseUrl, string embedServerUrl, bool nsfwChannel)
        {
            if (!message.Contains("reddit.com") || Links.CheckIfLinkHasBrackets(message)) return null;

            // Same rule as the Twitter flow: two "||" in the message means the user spoilered it.
            int firstBars = message.IndexOf("||");
            bool spoilered = firstBars != -1 && message.IndexOf("||", firstBars + 2) != -1;

            var fix = new RedditFix();
            var lines = new List<string>();
            var seen = new HashSet<string>();

            foreach (Match link in LinkPattern.Matches(message))
            {
                string path = link.Groups[1].Value;
                if (!seen.Add(path)) continue;   // same link twice in one message

                bool isShareLink = link.Groups[4].Success;
                string infoUrl = isShareLink
                    ? $"{embedServerUrl}/reddit/info/share/{link.Groups[3].Value}/{link.Groups[4].Value}"
                    : $"{embedServerUrl}/reddit/info/{link.Groups[2].Value}";

                RedditInfo? info = await GetJsonAsync<RedditInfo>(httpClient, infoUrl);
                if (info == null || !info.Ok) continue;   // can't read this post: leave it alone

                string url = baseUrl + path;
                string line = (spoilered || info.Nsfw && !nsfwChannel) ? $"|| {url} ||" : url;
                if (info.Nsfw) line = "**NSFW**\n" + line;
                lines.Add(line);

                if (info.NeedsMerge) fix.VideoIds.Add(info.Id);
            }

            if (lines.Count == 0) return null;

            fix.Content = string.Join("\n", lines);
            return fix;
        }

        /// <summary>Asks the embedserver to finish merging each video. Returns once they are ready (or have failed).</summary>
        public static async Task PrepareVideosAsync(RedditFix fix, HttpClient httpClient, string embedServerUrl)
        {
            foreach (string id in fix.VideoIds)
            {
                try
                {
                    using var response = await httpClient.GetAsync($"{embedServerUrl}/reddit/prepare/{id}");
                }
                catch (Exception e)
                {
                    // Not fatal: the embed falls back to the silent video.
                    Console.WriteLine($"Reddit prepare failed for {id}: {e.Message}");
                }
            }
        }

        private static async Task<T?> GetJsonAsync<T>(HttpClient httpClient, string url) where T : class
        {
            try
            {
                using var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Request to embedserver failed ({url}): {e.Message}");
                return null;
            }
        }
    }
}
