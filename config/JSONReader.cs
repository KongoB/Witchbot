using Newtonsoft.Json;

namespace vxtwitterbot.config
{
    public class JSONReader
    {
        public string token { get; set; }

        public async Task ReadJSON()
        {
            using (StreamReader sr = new StreamReader("config.json"))
            {
                string json = await sr.ReadToEndAsync();
                JsonStructure data = JsonConvert.DeserializeObject<JsonStructure>(json);

                this.token = data.token;
            }
        }
    }

    internal sealed class JsonStructure
    {
        public string token { get; set; }
    }
}
