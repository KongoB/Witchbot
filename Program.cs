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
                e.HandleMessageCreated((client, args) => Services.LinkMessageFix(client, args, jsonReader));
                e.HandleMessageCreated(Services.Utility);
                e.HandleMessageCreated(Services.DeleteMessage);
            });

            DiscordClient client = builder.Build();
            await client.ConnectAsync();
            await Task.Delay(-1);

        }
    }
}