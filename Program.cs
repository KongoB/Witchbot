using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using vxtwitterbot.config;
using vxtwitterbot.Functions;

namespace vxTwitterBot
{
    class Program
    {
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }
        private static SlashCommandsExtension SlashCommands { get; set; }
        
        private static JSONReader jsonReader = new JSONReader();
        private static bool jsonRead = false;

        static async Task Main(string[] args)
        {
            jsonReader = new JSONReader();
            await jsonReader.ReadJSON();
            
            //SlashCommands.RegisterCommands<vxtwitterbot.Functions.SlashCommands>(588533489146658816);
            
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
            Client.MessageCreated += DeleteMessage;
            //Client.MessageCreated += Clear_cache;


            var slash = Client.UseSlashCommands();
            slash.RegisterCommands<vxtwitterbot.Functions.SlashCommands>();



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
                Sites.PixivFunc(e);
                //Sites.TiktokFunc(e);

                await e.Message.ModifyEmbedSuppressionAsync(hideEmbeds: true);
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
                    await e.Message.RespondAsync("You can include \"nofix\" in your message to make me ignore a message");
                }
                else
                {
                    await e.Message.RespondAsync("Ping!");
                }
            }

        }

        private static async Task DeleteMessage(DiscordClient sender, MessageCreateEventArgs e)
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

        private static async Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            await Client.UpdateStatusAsync(new DiscordActivity("XD"));
        }
        


        //private static async Task Clear_cache(DiscordClient sender, MessageCreateEventArgs e)
        //{
        //    if (e.Message.Content.Contains("clearcache"))
        //    {
        //        var globalCommands = await Client.GetGlobalApplicationCommandsAsync();

        //        foreach (var command in globalCommands)
        //        {
        //            await e.Channel.SendMessageAsync(command.Name);
        //            await Client.DeleteGlobalApplicationCommandAsync(command.Id);
        //        }

        //        var guildCommands = await Client.GetGuildApplicationCommandsAsync(635562812864725012);
        //        var guildCommands2 = await Client.GetGuildApplicationCommandsAsync(588533489146658816);

        //        foreach (var command in guildCommands)
        //        {
        //            await e.Channel.SendMessageAsync(command.Name);
        //            await Client.DeleteGuildApplicationCommandAsync(635562812864725012, command.Id);
        //        }
        //        foreach (var command in guildCommands2)
        //        {
        //            await e.Channel.SendMessageAsync(command.Name);
        //            await Client.DeleteGuildApplicationCommandAsync(588533489146658816, command.Id);
        //        }
        //    }
        //}

    }
}