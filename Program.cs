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
            Client.MessageCreated += HueDolph;

            

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
                !jsonReader.IgnoredWebsites.Any(item => message.Contains(item))) 

            { 
                if (message.Contains("twitter") || message.Contains("x.com"))
                {

                    if (Links.CheckIfLinkHasBrackets(e.Message.Content) && !e.Message.MentionedUsers.Any())
                    {
                        return;
                    }

                    string output = Links.TwitterLink(Links.FilterLinkFromMessage(message, "twitter"));

                    output = Links.CheckIfSpoilerTag(output, message);

                    await e.Channel.SendMessageAsync(e.Message.MentionedUsers.ToString() + " " + output);
                    await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);

                }
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



        }

        private static async Task HueDolph(DiscordClient sender, MessageCreateEventArgs e)
        {

            if (e.Message.Author.Id == 677243986087378951)
            {
                if (e.Message.MentionedUsers.Contains(sender.CurrentUser))
                {
                    await e.Message.RespondAsync("https://cdn.discordapp.com/attachments/550834390272573450/1161500716788092949/avatars-253MmMf9QZzxVBJi-rvlyeg-t500x500.png?ex=653886d6&is=652611d6&hm=b45908aa96b35966fa35fc7a33348df8cbc9fb1b6410979f2cbb919526164a41&");

                    //https://cdn.discordapp.com/attachments/550834390272573450/1161500716788092949/avatars-253MmMf9QZzxVBJi-rvlyeg-t500x500.png?ex=653886d6&is=652611d6&hm=b45908aa96b35966fa35fc7a33348df8cbc9fb1b6410979f2cbb919526164a41& - DOLPHO
                    //https://cdn.discordapp.com/attachments/588688292506107905/1160784277546471444/dolph.png - DOLPH

                }

                if (e.Message.Content.Contains(":ASgibsHiss:"))
                {
                    await e.Message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":FL_Scared:"));
                }
            }

        }

        private static async Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            await Client.UpdateStatusAsync(new DiscordActivity("XD"));
        }
        


    }
}