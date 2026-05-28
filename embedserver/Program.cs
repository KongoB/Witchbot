using System.Text.Json;
using embedserver;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; embedserver/1.0)");

app.MapGet("/tweet", async (string url) => 
{

    return await TweetHandler.HandleAsync(url, httpClient);

    

});

app.MapGet("/oembed", (string authorName, string screenName, string avatarUrl) =>
{
    var oembed = new
    {
        author_name = $"{authorName} (@{screenName})",
        author_url = $"https://x.com/{screenName}",
        provider_name = "Bongo",
        provider_url = "",
        thumbnail_url = avatarUrl,
        type = "link",
        version = "1.0"
    };
});


app.Run();
