using Newtonsoft.Json;

namespace vxtwitterbot.config
{
    public class JSONReader
    {
        public string token { get; set; } = "";
        public string[] FilteredWebsites { get; set; } = [];
        public string[] IgnoredWebsites { get; set; } = [];
        public string[] IgnoredPeople { get; set; } = [];
        public string BaseUrl { get; set; } = "https://witchbot.net";

        // Where the bot reaches the embedserver. Inside Docker that is the service name;
        // for local testing set "EmbedServerUrl": "http://localhost:5031" in config/settings.json.
        public string EmbedServerUrl { get; set; } = "http://embedserver:8080";


        public async Task ReadJSON()
        {
            using (StreamReader sr = new StreamReader("config/websites.json"))
            {
                JsonStructure data = Deserializer(await sr.ReadToEndAsync(), "websites.json");

                this.FilteredWebsites = data.FilteredWebsites;
                this.IgnoredWebsites = data.IgnoredWebsites;
            }
            using (StreamReader sr = new StreamReader("config/token.json"))
            {
                JsonStructure data = Deserializer(await sr.ReadToEndAsync(), "token.json");

                this.token = data.token;
            }
            using (StreamReader sr = new StreamReader("config/ignored.json"))
            {
                JsonStructure data = Deserializer(await sr.ReadToEndAsync(), "ignored.json");

                this.IgnoredPeople = data.IgnoredPeople;
            }
            using (StreamReader sr = new StreamReader("config/settings.json"))
            {
                JsonStructure data = Deserializer(await sr.ReadToEndAsync(), "settings.json");

                this.BaseUrl = data.BaseUrl;
                if (!string.IsNullOrWhiteSpace(data.EmbedServerUrl))
                {
                    this.EmbedServerUrl = data.EmbedServerUrl.TrimEnd('/');
                }
            }
        }

        internal static JsonStructure Deserializer(string json, string fileName)
        {
            try
            {
                JsonStructure? data = JsonConvert.DeserializeObject<JsonStructure>(json) ?? throw new Exception();
                return data;
            }
            catch (JsonException)
            {

                throw new Exception($"Misconfigured JSON file {fileName} in /config");
            }
        }
    }




    internal sealed class JsonStructure
    {
        public string token { get; set; } = "";
        public string[] FilteredWebsites { get; set; } = [];
        public string[] IgnoredWebsites { get; set; } = [];
        public string[] IgnoredPeople { get; set; } = [];
        public string BaseUrl { get; set; } = "";
        public string EmbedServerUrl { get; set; } = "";
    }
}

