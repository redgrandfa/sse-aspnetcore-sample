using Microsoft.AspNetCore.Mvc;
using SseServer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<MessageHub, MessageHub>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAny", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            // .AllowCredentials()
            ;
    });
});

var app = builder.Build();

app.UseCors("AllowAny");
app.UseDefaultFiles();  // for wwwroot/index.html
app.UseStaticFiles();

app.MapGet("/api/connect", async (HttpContext context, MessageHub messageHub) =>
{
    context.Response.Headers.Add("Content-Type", "text/event-stream");

    var writer = new StreamWriter(context.Response.Body);
    var clientId = messageHub.RegisterClient(writer);

    try
    {
        // heartbeat
        while (!context.RequestAborted.IsCancellationRequested)
        {
            await writer.WriteAsync(":\n\n");
            await writer.FlushAsync();
            await Task.Delay(10000, context.RequestAborted);
        }
    }
    catch (TaskCanceledException) { }
    finally
    {
        messageHub.UnregisterClient(clientId);
    }
});

app.MapPost("/api/push", async ([FromBody] string message, MessageHub messageHub) =>
{
    await messageHub.BroadcastAsync(message);
    return TypedResults.NoContent(); // 204
});

app.Run();
