using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace vxtwitterbot.Functions
{
    public class SlashCommands : ApplicationCommandModule
    {
        [SlashCommand("help", "Lists available functions")]
        public static async Task HelpComand(InteractionContext ctx)
        {

            var embeded = new DiscordEmbedBuilder
            {
                Title = "Discord bot for fixing embeds",
                Description = "Here are the available commands:",
                Color = DiscordColor.Azure,
            };

            embeded.AddField("/delete", "Deletes a message. Provde a message ID of the bot message you want deleted. You can also reply to the message with the word delete");

            await ctx.CreateResponseAsync(embed: embeded);
        }

        [SlashCommand("delete", "Deletes a message sent from the bot")]
        public static async Task DeleteMessage(InteractionContext ctx, [Option("messageID", "Message ID of the bot message")] string messageID = null){

            if (string.IsNullOrEmpty(messageID))
            {
                await ctx.CreateResponseAsync("Please provide a valid message ID.", true);
                return;
            }

            try
            {
                
                var message = await ctx.Channel.GetMessageAsync(ulong.Parse(messageID));

                //remove this after some time
                if (message.Reference?.Message.Id == null)
                {
                    await ctx.CreateResponseAsync($"Only messages from the new format can be deleted, sry! Ask <@{140840867941449728}> to delete it for you", true);
                    return;
                }
                var original = await ctx.Channel.GetMessageAsync(message.Reference.Message.Id);

                if (message.Author.Id == ctx.Client.CurrentUser.Id && 
                    original.Author.Id == ctx.User.Id)
                {

                    await message.DeleteAsync();
                    await ctx.CreateResponseAsync($"Message has been deleted.", true);
                }
                else
                {
                    await ctx.CreateResponseAsync($":furinasus:", true);
                }
            }
            catch
            {
                await ctx.CreateResponseAsync("Could not find the message. Please check the message ID and make sure the command is run in the same channel.", true);
            }
        }
    }
}
