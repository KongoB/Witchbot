using System.Net.Mime;
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

                string? output = await Sites.TwitterFunc(message, httpClient, jsonReader.BaseUrl, jsonReader.EmbedServerUrl);

                if (!string.IsNullOrEmpty(output))
                {
                    if (string.IsNullOrEmpty(output)) return;

                    var builder = new DiscordMessageBuilder()
                        .WithContent($"{output}\nPosted by {e.Message.Author?.Mention}")
                        .WithAllowedMentions(Mentions.None);

                    await Respond(e, builder);

                    await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);
                    await Task.Delay(2000);
                    if (e.Message.Embeds.Any())
                    {
                        await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);
                    }
                }
                else
                {
                    await RedditMessageFix(e, message, jsonReader, httpClient);
                }
                // await Sites.PixivFunc(e, messageContent);
                //Sites.TiktokFunc(e);


            }
        }

        private static async Task RedditMessageFix(MessageCreatedEventArgs e, string message, JSONReader jsonReader, HttpClient httpClient)
        {
            DiscordMessage? reply = null;
            bool posted = false;
            bool nsfwChannel = e.Channel.IsNSFW || (e.Channel.Parent?.IsNSFW ?? false);

            try
            {
                RedditFix? fix = await Reddit.BuildFixAsync(message, httpClient, jsonReader.BaseUrl, jsonReader.EmbedServerUrl, nsfwChannel);
                if (fix == null) return;   // nothing we can improve: stay silent

                string footer = $"\nPosted by {e.Message.Author?.Mention}";
                var finalMessage = new DiscordMessageBuilder()
                    .WithContent(fix.Content + footer)
                    .WithAllowedMentions(Mentions.None);

                if (fix.VideoIds.Count > 0)
                {
                    // Videos take a few seconds to merge. Show a placeholder, wait, then swap in the link,
                    // so Discord only looks at the link once the video file is ready.
                    var placeholder = new DiscordMessageBuilder()
                        .WithContent("⏳ Processing video…" + footer)
                        .WithAllowedMentions(Mentions.None);

                    reply = await e.Message.RespondAsync(placeholder);
                    await Reddit.PrepareVideosAsync(fix, httpClient, jsonReader.EmbedServerUrl);
                    await reply.ModifyAsync(finalMessage);
                }
                else
                {
                    reply = await e.Message.RespondAsync(finalMessage);
                }
                posted = true;

                await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);
                await Task.Delay(2000);
                if (e.Message.Embeds.Any())
                {
                    await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Reddit fix failed: {ex.GetType().Name}: {ex.Message}");

                // Don't leave a "Processing video" message hanging if something broke half-way.
                if (reply != null && !posted)
                {
                    try { await reply.DeleteAsync(); } catch { }
                }
            }
        }

        private static async Task Respond(MessageCreatedEventArgs e, DiscordMessageBuilder message)
        {
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
                    await e.Message.RespondAsync("You can include \"nofix\" in your message to make me ignore a message\nReply to a message I posted with the word \"delete\" to delete my message (it has to be a reply to your message).");
                }
                else
                {
                    await e.Message.RespondAsync("Ping!");
                }
            }
        }

        public static async Task DeleteMessage(DiscordClient sender, MessageCreatedEventArgs e)
        {

            if (e.Message.Content.Trim().ToLower() == "delete" 
                && e.Message.Reference != null)
            {
                var frickMyDisgustingChungusLife = await e.Channel.GetMessageAsync(e.Message.Reference.Message.Id);

                if (frickMyDisgustingChungusLife is null) return;
                if (frickMyDisgustingChungusLife.Author is null) return;

                if(frickMyDisgustingChungusLife.Author.Id == sender.CurrentUser.Id 
                    && frickMyDisgustingChungusLife.MentionedUsers.Any(u => u.Id == e.Message.Author?.Id))
                {
                    await frickMyDisgustingChungusLife.DeleteAsync();
                }
            }
        }

    }
}