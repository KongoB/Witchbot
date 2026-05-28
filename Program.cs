using DSharpPlus;
using DSharpPlus.Entities;
using vxtwitterbot.config;
using vxtwitterbot.Functions;
using DSharpPlus.Commands;
using DSharpPlus.EventArgs;


namespace vxTwitterBot
{
    class Program
    {        
        private static JSONReader jsonReader = new JSONReader();

        static async Task Main(string[] args)
        {
            await jsonReader.ReadJSON();
            
            //SlashCommands.RegisterCommands<vxtwitterbot.Functions.SlashCommands>(588533489146658816);
            
            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(
                jsonReader.token,
                DiscordIntents.MessageContents | DiscordIntents.GuildMessages | DiscordIntents.Guilds
            );

            builder.ConfigureEventHandlers (e =>
            {
                e.HandleMessageCreated(LinkMessageFix);
                e.HandleMessageCreated(Utility);
                e.HandleMessageCreated(DeleteMessage);
            });



            DiscordClient client = builder.Build();
            await client.ConnectAsync();
            await Task.Delay(-1);

        }


        private static async Task LinkMessageFix(DiscordClient sender, MessageCreatedEventArgs e)
        {

            string message = e.Message.Content;


            if (jsonReader.FilteredWebsites.Any(item => message.Contains(item)) &&
                !jsonReader.IgnoredWebsites.Any(item => message.Contains(item)) &&
                !message.Contains("nofix") &&
                !jsonReader.IgnoredPeople.Contains(e.Author.Id.ToString()))

            {
                await Sites.TwitterFunc(e);
                await Sites.RedditFunc(e);
                await Sites.PixivFunc(e);
                //Sites.TiktokFunc(e);

                await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);
                await Task.Delay(2000);
                if (e.Message.Embeds.Any())
                {
                    await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);
                }
            }
        }

        private static async Task Utility(DiscordClient sender, MessageCreatedEventArgs e)
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

        private static async Task DeleteMessage(DiscordClient sender, MessageCreatedEventArgs e)
        {

            if (e.Message.Content.ToLower().Contains("delete") && e.Message.Reference != null)
            {
                if (e.Message.ReferencedMessage.Reference == null)
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