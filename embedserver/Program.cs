using embedserver;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
string baseUrl = builder.Configuration["BaseUrl"] ?? "https://witchbot.net";
var app = builder.Build();

app.UseStaticFiles();

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; Witchbot/1.0)");

var cache = new MemoryCache(new MemoryCacheOptions());

app.MapGet("/{username}/status/{tweetId}", async (string username, string tweetId) =>
{
    return await TweetHandler.HandleAsync(username, tweetId, httpClient, cache, baseUrl);
});

app.MapGet("/check/{username}/{tweetId}", async (string username, string tweetId) =>
{
    return await TweetHandler.HasVideo(username, tweetId, httpClient, cache);
});

app.MapGet("/oembed", (string text, string status, string author, string? provider) =>
{

    Console.WriteLine($"oEmbed request received");
    string statusUrl = $"https://x.com/{author}/status/{status}";

    return Results.Json(new
    {
        author_name = text,
        author_url = statusUrl, 
        provider_name = string.IsNullOrEmpty(provider) ? "Witch" : provider,  
        provider_url = string.IsNullOrEmpty(provider) ? "https://witchbot.net" : statusUrl,
        title = "Witch",
        type = "rich",
        version = "1.0"
    });
});

app.MapGet("/users/{username}/statuses/{tweetId}", async (string username, string tweetId) =>
{
    Console.WriteLine($"Activity JSON request for {tweetId}");
    return await TweetHandler.HandleActivityAsync(username, tweetId, httpClient, cache, baseUrl);
});

app.MapGet("/api/v1/statuses/{tweetId}", async (string tweetId) =>
{
    Console.WriteLine($"Mastodon API request for {tweetId}");
    return await TweetHandler.HandleActivityAsync("", tweetId, httpClient, cache, baseUrl);
});

app.MapGet("/gif/{tweetId}.webp", async (string tweetId) =>
{
    return await TweetHandler.HandleGifAsync(tweetId, httpClient, cache);
});




app.Run();
