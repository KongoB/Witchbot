using embedserver;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
string baseUrl = builder.Configuration["BaseUrl"] ?? "https://witchbot.net";
var app = builder.Build();

app.UseStaticFiles();

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; Witchbot/1.0)");

var cache = new MemoryCache(new MemoryCacheOptions());

// Reddit:Session / Reddit:UserAgent come from env vars Reddit__Session / Reddit__UserAgent (.env in Docker)
// or from appsettings.Development.json when running locally with dotnet run.
var reddit = new RedditClient(builder.Configuration["Reddit:Session"], builder.Configuration["Reddit:UserAgent"], cache);

app.Use(async (context, next) =>
{
    Console.WriteLine($"→ {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
    await next();
});

app.MapTwitterRoutes (httpClient, cache, baseUrl);
app.MapRedditRoutes (reddit, baseUrl);
app.MapMastodonRoutes (httpClient, cache, reddit, baseUrl);


app.Run();
