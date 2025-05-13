using StackExchange.Redis;
using WorkerCollector;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6381"));

// SignalR untuk WebSocket Hub
// builder.Services.AddSignalR(options =>
// {
//     options.EnableDetailedErrors = true;
//     options.MaximumReceiveMessageSize = 102400; // 100 KB
// });

// Tambahkan HostedService
builder.Services.AddHostedService<BufferedRedisSubscriberService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IGpsPublisher, RedisGpsPublisher>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.
        AllowAnyOrigin().
        AllowAnyMethod().
        AllowAnyHeader().
        AllowCredentials();
    });
});


var app = builder.Build();

app.UseRouting();

// app.UseEndpoints(endpoints =>
// {
//     endpoints.MapHub<GpsHub>("/gpsHub");
// });

// Tambahkan WebSocket endpoint
app.UseWebSockets();
app.Map("/gpsHub", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await WebSocketHandler.HandleWebSocket(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.Run();
