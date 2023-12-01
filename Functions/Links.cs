using System.Text.RegularExpressions;
using System.Text;

namespace vxtwitterbot.Functions
{
    public class Links
    {
        public static string FilterLinkFromMessage(string message, string platform)
        {
            StringBuilder links = new StringBuilder();

            string pattern = "invalid";

            if (platform == "twitter")
            {
                pattern = @"https?://(?:x|twitter).com/([^/]+/status/\d+)";
            }
            if (platform == "reddit")
            {
                if (message.Contains("/comments/"))
                {
                    pattern = @"(https?://)?(\w+)?\.?(reddit\.com/|redd\.it/)(r/\w+/)?(comments/)?(\w+)/(\w+)";
                }
                else if (message.Contains("/s/"))
                {
                    pattern = @"(https?://)?(\w+)?\.?(reddit\.com/|redd\.it/)(r/\w+/)?(s/)?(\w+)/(\w+)";
                }
            }
            if (platform == "tiktok")
            {
                pattern = @"^.*https:\/\/(?:m|www|vm)?\.?tiktok\.com\/((?:.*\b(?:(?:usr|v|embed|user|video)\/|\?shareId=|\&item_id=)(\d+))|\w+)";
            }

            if (platform == "reddit" && pattern == "invalid")
            {
                return pattern;
            }


            Regex regex = new Regex(pattern);

            MatchCollection match = regex.Matches(message);



            bool firstMessage = true;


            foreach (var link in match)
            {
                if (firstMessage)
                {
                    links.Append(link.ToString());
                    firstMessage = false;
                }
                else
                {
                    links.Append("\n" + link.ToString());
                }
            }

            return links.ToString();
        }

        public static string TwitterLink(string link)
        {
            string vxtwitterlink = null;

            if (link.Contains("twitter.com"))
            {
                vxtwitterlink = link.Replace("twitter.com", "vxtwitter.com");
            }
            if ((link.Contains("x.com")))
            {
                vxtwitterlink = link.Replace("x.com", "vxtwitter.com");
            }

            return vxtwitterlink;
        }

        public static string RedditLink(string link)
        {

            string xRedditLink = link.Replace("reddit.com", "rxddit.com");

            return xRedditLink;
        }

        public static string TiktokLink(string link)
        {
            string xTwitterLink = link.Replace("tiktok.com", "tiktxk.com");

            return xTwitterLink;
        }

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
