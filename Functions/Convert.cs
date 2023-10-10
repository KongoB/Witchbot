using DSharpPlus.Entities;
using System.Threading.Channels;
using System.Text.RegularExpressions;
using System.Text;

namespace vxtwitterbot.Functions
{
    public class Links
    {

        public static string FilterLinkFromMessage (string message)
        {
            StringBuilder links = new StringBuilder ();

            string pattern = @"https?://(?:x|twitter).com/([^/]+/status/\d+)";
            Regex regex = new Regex (pattern);

            MatchCollection match = regex.Matches (message);
            bool firstMessage = true;

            foreach (var link in match)
            {
                if (firstMessage)
                {
                    links.Append (link.ToString ());
                    firstMessage = false;
                }
                else
                {
                    links.Append ("\n" + link.ToString ());
                }
            }

            return links.ToString ();
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

    }
}
