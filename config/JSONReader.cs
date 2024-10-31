using Newtonsoft.Json;

namespace vxtwitterbot.config
{
    public class JSONReader
    {
        public string token { get; set; }
        public string[] FilteredWebsites { get; set; }
        public string[] IgnoredWebsites { get; set; }


        public async Task ReadJSON()
        {
            using (StreamReader sr = new StreamReader("config/websites.json"))
            {
                string json = await sr.ReadToEndAsync();
                JsonStructure data = JsonConvert.DeserializeObject<JsonStructure>(json);

                this.token = data.token;
                this.FilteredWebsites = data.FilteredWebsites;
                this.IgnoredWebsites = data.IgnoredWebsites;
            }
        }
    }

    internal sealed class JsonStructure
    {
        public string token { get; set; }
        public string[] FilteredWebsites { get; set; }
        public string[] IgnoredWebsites { get; set; }
    }
}
