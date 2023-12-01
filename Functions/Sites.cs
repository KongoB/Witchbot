using DSharpPlus.EventArgs;

namespace vxtwitterbot.Functions
{
    public class Sites
    {

        public static async void TwitterFunc(MessageCreateEventArgs e)
        {
            string message = e.Message.Content;

            if (message.Contains("twitter") || message.Contains("x.com"))
            {

                if (Links.CheckIfLinkHasBrackets(e.Message.Content))
                {
                    return;
                }

                string output = Links.TwitterLink(Links.FilterLinkFromMessage(message, "twitter"));

                output = Links.CheckIfSpoilerTag(output, message);

                await e.Channel.SendMessageAsync(output);
                await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);

            }
        }

        public static async void RedditFunc (MessageCreateEventArgs e)
        {
            string message = e.Message.Content;

            if (message.Contains("reddit"))
            {
                string filteredMessage = Links.FilterLinkFromMessage(message, "reddit");
                string output = Links.RedditLink(filteredMessage);

                output = Links.CheckIfSpoilerTag(output, message);

                if (filteredMessage != "invalid")
                {
                    await e.Channel.SendMessageAsync(output);
                    await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);
                }
            }
        }

        public static async void TiktokFunc (MessageCreateEventArgs e)
        {
            string message = e.Message.Content;

            if (message.Contains("tiktok"))
            {
                string filteredMessage = Links.FilterLinkFromMessage(message, "tiktok");
                string output = Links.TiktokLink(filteredMessage);

                output = Links.CheckIfSpoilerTag(output, message);

                await e.Channel.SendMessageAsync(output);
                await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);
            }
        }

    }
}
