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



app.Run();
