using System.Text.RegularExpressions;
using System.Text;
using System.Linq;

namespace vxtwitterbot.Functions
{
    public class Links
    {
        public static MatchCollection? FilterLinkFromMessage(string message, string platform)
        {
            string pattern = "invalid";

            if (platform == "twitter")
            {
                pattern = @"https?://(?:x|twitter).com/([^/]+)/status/(\d+)";
            }
            if (platform == "reddit")
            {
                pattern = @"(https?://)?(\w+)?\.?(reddit\.com/|redd\.it/)(r/\w+/)?(?:s|comments/)?(\w+)/(\w+)";
            }
            if (platform == "tiktok")
            {
                pattern = @"^.*https:\/\/(?:m|www|vm)?\.?tiktok\.com\/((?:.*\b(?:(?:usr|v|embed|user|video)\/|\?shareId=|\&item_id=)(\d+))|\w+)";
            }
            if (platform == "pixiv")
            {
                pattern = @"https?://?(\w+)?\.?pixiv\.net\/(?:[a-z]{2}\/)?artworks\/\d+";
            }

            if (pattern == "invalid")
            {
                return null;
            }

            Regex regex = new Regex(pattern);

            MatchCollection match = regex.Matches(message);

            return match;
        }

        public static string TwitterLink(MatchCollection match)
        {
            StringBuilder links = new StringBuilder();

            bool firstMessage = true;

            foreach (Match link in match)
            {
                if (firstMessage)
                {
                    links.Append($"https://www.bongobot.online/{link.Groups[1].Value}/status/{link.Groups[2].Value}");
                    firstMessage = false;
                }
                else
                {
                    links.Append($"\nhttps://www.bongobot.online/{link.Groups[1].Value}/status/{link.Groups[2].Value}");
                }
            }

            return links.ToString();
        }

        // public static string RedditLink(string link)
        // {

        //     string xRedditLink = link.Replace("reddit.com", "rxddit.com");

        //     xRedditLink += ("\nDue to limitations with Reddit, videos might be without audio.");

        //     return xRedditLink;
        // }

        // public static string TiktokLink(string link)
        // {
        //     string xTwitterLink = link.Replace("tiktok.com", "tiktxk.com");

        //     return xTwitterLink;
        // }

        // public static string PixivLink(string link)
        // {
        //     string xPixivLink = link.Replace("pixiv.net", "phixiv.net");

        //     return xPixivLink;
        // }

        public static bool CheckIfLinkHasBrackets(string message)
        {
            if (message.Contains("<https://"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string CheckIfSpoilerTag(string links, string message)
        {
            bool containsSpoilers = message.Contains("||") && message.IndexOf("||", message.IndexOf("||") + 1) != -1;

            if (containsSpoilers)
            {
                return "||" + links + "||";
            }
            else
            {
                return links;
            }

        }

    }
}
