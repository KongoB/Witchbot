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

        static async Task Main(string[] args)
        {
            var jsonReader = new JSONReader();
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
            Client.MessageCreated += TwitterMessageFix;
            Client.MessageCreated += HueDolph;



            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static async Task TwitterMessageFix(DiscordClient sender, MessageCreateEventArgs e)
        {
            if ((e.Message.Content.Contains("https://twitter.com") || 
                e.Message.Content.Contains("https://x.com")) && 
                !e.Message.Content.Contains("https://vxtwitter.com") &&
                !e.Message.Content.Contains("https://fxtwitter.com"))
            {
                
                if (!Links.CheckIfLinkHasBrackets(e.Message.Content))
                {
                    await e.Channel.SendMessageAsync(Links.TwitterLink(Links.FilterLinkFromMessage(e.Message.Content)));
                    await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);
                }

                /*var message = new DiscordMessageBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Changing Twitter link to vxtwitter")
                    .WithDescription($"**Message sent by: {e.Author.Username}**")
                    .WithThumbnail(e.Author.AvatarUrl)
                    .WithColor(DiscordColor.Turquoise));*/
                //await e.Channel.SendMessageAsync(message);
                //await e.Message.DeleteAsync();
            }
        }

        private static async Task HueDolph(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Message.Author.Id == 677243986087378951 && e.Message.MentionedUsers.Contains(sender.CurrentUser))
            {
                await e.Message.RespondAsync("https://cdn.discordapp.com/attachments/550834390272573450/1161500716788092949/avatars-253MmMf9QZzxVBJi-rvlyeg-t500x500.png?ex=653886d6&is=652611d6&hm=b45908aa96b35966fa35fc7a33348df8cbc9fb1b6410979f2cbb919526164a41&");

                //https://cdn.discordapp.com/attachments/550834390272573450/1161500716788092949/avatars-253MmMf9QZzxVBJi-rvlyeg-t500x500.png?ex=653886d6&is=652611d6&hm=b45908aa96b35966fa35fc7a33348df8cbc9fb1b6410979f2cbb919526164a41& - DOLPHO
                //https://cdn.discordapp.com/attachments/588688292506107905/1160784277546471444/dolph.png - DOLPH

            }

            if (e.Message.Author.Id == 677243986087378951 && e.Message.Content.Contains(":ASgibsHiss:"))
            {
                await e.Message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":FL_Scared:"));
            }

        }

        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }


        
    }
}