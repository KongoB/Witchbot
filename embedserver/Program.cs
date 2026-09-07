using embedserver;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
string baseUrl = builder.Configuration["BaseUrl"] ?? "https://witchbot.net";
var app = builder.Build();

app.UseStaticFiles();

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; Witchbot/1.0)");

var cache = new MemoryCache(new MemoryCacheOptions());

app.Use(async (context, next) =>
{
    Console.WriteLine($"→ {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");
    await next();
});

app.MapTwitterRoutes (httpClient, cache, baseUrl);
app.MapRedditRoutes (httpClient, cache, baseUrl);


app.Run();
