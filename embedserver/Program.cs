using System.Text.Json;
using embedserver;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; Bongobot/1.0)");

var cache = new MemoryCache(new MemoryCacheOptions());

app.MapGet("/{username}/status/{tweetId}", async (string username, string tweetId) => 
{
    return await TweetHandler.HandleAsync(username, tweetId, httpClient, cache);
});

app.MapGet("/check/{username}/{tweetId}", async (string username, string tweetId) =>
{
    return await TweetHandler.HasVideo(username, tweetId, httpClient, cache);
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

    return Results.Json(oembed);
});




app.Run();
