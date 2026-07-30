using DSharpPlus;
using vxtwitterbot.config;
using vxtwitterbot.Functions;


namespace vxTwitterBot
{
    class Program
    {        
        private static JSONReader jsonReader = new JSONReader();
        

        static async Task Main(string[] args)
        {
            await jsonReader.ReadJSON();
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; Bongobot/1.0)");
            
            //SlashCommands.RegisterCommands<vxtwitterbot.Functions.SlashCommands>(588533489146658816);
            
            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(
                jsonReader.token,
                DiscordIntents.MessageContents | DiscordIntents.GuildMessages | DiscordIntents.Guilds
            );

            builder.ConfigureEventHandlers (e =>
            {
                e.HandleMessageCreated((client, args) => Services.LinkMessageFix(client, args, jsonReader, httpClient));
                e.HandleMessageCreated(Services.Utility);
                e.HandleMessageCreated(Services.DeleteMessage);
            });

            DiscordClient client = builder.Build();
            await client.ConnectAsync();
            await Task.Delay(-1);

        }
    }
}