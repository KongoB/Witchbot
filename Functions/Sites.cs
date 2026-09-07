using System.Text.RegularExpressions;

namespace vxtwitterbot.Functions
{
    public class Sites
    {

        public static async Task<string?> TwitterFunc(string message, HttpClient httpClient, string baseUrl)
        {

            if (message.Contains("twitter") || message.Contains("x.com"))
            {
                MatchCollection? filteredLinks = Links.FilterLinkFromMessage(message, "twitter");


                if (Links.CheckIfLinkHasBrackets(message) || filteredLinks == null || filteredLinks.Count == 0)
                {
                    return null;
                }

                string? output = await Links.ProcessTwitterLinks(filteredLinks, httpClient, baseUrl);

                if (string.IsNullOrEmpty(output))
                {
                    return null;
                }

                output = Links.CheckIfSpoilerTag(output, message);
                

                return output;

            }
            return null;
        }

        

        // public static async Task RedditFunc (MessageCreatedEventArgs e, string message)
        // {

        //     if (message.Contains("reddit"))
        //     {
        //         string filteredMessage = Links.FilterLinkFromMessage(message, "reddit");
        //         string output = Links.RedditLink(filteredMessage);

        //         output = Links.CheckIfSpoilerTag(output, message);

        //         await e.Message.RespondAsync(output);
                
        //     }
        // }

        // public static async Task TiktokFunc (MessageCreatedEventArgs e, string message)
        // {

        //     if (message.Contains("tiktok"))
        //     {
        //         string filteredMessage = Links.FilterLinkFromMessage(message, "tiktok");
        //         string output = Links.TiktokLink(filteredMessage);

        //         output = Links.CheckIfSpoilerTag(output, message);

        //         await e.Message.RespondAsync(output);
        //     }
        // }

        // public static async Task PixivFunc (MessageCreatedEventArgs e, string message)
        // {

        //     if (message.Contains("pixiv"))
        //     {
        //         string filteredMessage = Links.FilterLinkFromMessage(message, "pixiv");
        //         string output = Links.PixivLink(filteredMessage);

        //         output = Links.CheckIfSpoilerTag(output, message);

        //         await e.Message.RespondAsync(output);
        //     }
        // }

    }
}
