using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using vxtwitterbot.config;

namespace vxtwitterbot.Functions
{
    public class Services
    {

        public static async Task LinkMessageFix(DiscordClient sender, MessageCreatedEventArgs e, JSONReader jsonReader, HttpClient httpClient)
        {

            string message = e.Message.Content;


            if (jsonReader.FilteredWebsites.Any(item => message.Contains(item)) &&
                !jsonReader.IgnoredWebsites.Any(item => message.Contains(item)) &&
                !message.Contains("nofix") &&
                !jsonReader.IgnoredPeople.Contains(e.Author.Id.ToString()))

            {

                await Respond(e,await Sites.TwitterFunc(message, httpClient, jsonReader.BaseUrl));
                // await Sites.RedditFunc(e, messageContent);
                // await Sites.PixivFunc(e, messageContent);
                //Sites.TiktokFunc(e);

                await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);
                await Task.Delay(2000);
                if (e.Message.Embeds.Any())
                {
                    await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);
                }
            }
        }

        private static async Task Respond(MessageCreatedEventArgs e, string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            await e.Message.RespondAsync(message);

        }

        public static async Task Utility(DiscordClient sender, MessageCreatedEventArgs e)
        {

            if (e.Message.Content.Contains(":ASgibsHiss:"))
            {
                await e.Message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":FL_Scared:"));
            }

            if (e.Message.MentionedUsers.Contains(sender.CurrentUser) && e.Message.Reference == null)
            {
                if (e.Message.Content.Contains("help"))
                {
                    await e.Message.RespondAsync("You can include \"nofix\" in your message to make me ignore a message");
                }
                else
                {
                    await e.Message.RespondAsync("Ping!");
                }
            }

        }

        public static async Task DeleteMessage(DiscordClient sender, MessageCreatedEventArgs e)
        {

            if (e.Message.Content.ToLower().Contains("delete") && e.Message.Reference != null)
            {
                if (e.Message.ReferencedMessage?.Reference == null)
                {
                    return;
                }

                var message = await e.Channel.GetMessageAsync(e.Message.ReferencedMessage.Reference.Message.Id);


                if (e.Message.ReferencedMessage.Author == sender.CurrentUser &&
                    message.Author == e.Author)
                {
                    await e.Message.ReferencedMessage.DeleteAsync();
                }
            }


        }


    }
}