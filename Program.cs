using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using vxtwitterbot.config;
using vxtwitterbot.Functions;

namespace vxTwitterBot
{
    class Program
    {
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }

        private static JSONReader jsonReader = new JSONReader();
        private static bool jsonRead = false;

        static async Task Main(string[] args)
        {
            jsonReader = new JSONReader();
            await jsonReader.ReadJSON();
            
            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.MessageContents | DiscordIntents.GuildMessages | DiscordIntents.Guilds,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            Client = new DiscordClient(discordConfig);

            Client.Ready += Client_Ready;
            Client.MessageCreated += LinkMessageFix;
            Client.MessageCreated += Utility;

            

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }


        private static async Task LinkMessageFix(DiscordClient sender, MessageCreateEventArgs e)
        {

            if (!jsonRead)
            {
                jsonReader = new JSONReader();
                await jsonReader.ReadJSON();
            }

            string message = e.Message.Content;

            if (jsonReader.FilteredWebsites.Any(item => message.Contains(item)) &&
                !jsonReader.IgnoredWebsites.Any(item => message.Contains(item)) &&
                !message.Contains("nofix"))

            {
                Sites.TwitterFunc(e);
                Sites.RedditFunc(e);
                //Sites.TiktokFunc(e);
            }
        }

        private static async Task Utility(DiscordClient sender, MessageCreateEventArgs e)
        {

            if (e.Message.Content.Contains(":ASgibsHiss:"))
            {
                await e.Message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":FL_Scared:"));
            }

            if (e.Message.MentionedUsers.Contains(sender.CurrentUser) && e.Message.Reference == null)
            {
                if (e.Message.Content.Contains("help"))
                {
                    await e.Message.RespondAsync("You can inlude \"nofix\" in your message to make me ignore a message");
                }
                else
                {
                    await e.Message.RespondAsync("Ping!");
                }
            }

        }

        private static async Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            await Client.UpdateStatusAsync(new DiscordActivity("XD"));
        }
        


    }
}