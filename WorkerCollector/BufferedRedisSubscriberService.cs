using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.WebSockets;

public class BufferedRedisSubscriberService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<BufferedRedisSubscriberService> _logger;
    private readonly ConcurrentDictionary<string, GpsData> _gpsBuffer;
    private Timer? _timer;

    public BufferedRedisSubscriberService(
        IConnectionMultiplexer redis,
        ILogger<BufferedRedisSubscriberService> logger)
    {
        _redis = redis;
        _logger = logger;
        _gpsBuffer = new ConcurrentDictionary<string, GpsData>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();

        // Subscribe ke Redis channel
        await subscriber.SubscribeAsync("gps_update", async (channel, message) =>
        {
            try
            {
                var unitData = JsonSerializer.Deserialize<UnitData>(message.ToString());
                if (unitData != null)
                {
                    // Convert UnitData ke GpsData
                    var gpsData = new GpsData
                    {
                        UnitNo = unitData.UnitNo,
                        Lat = double.Parse(unitData.GpsLat),
                        Lon = double.Parse(unitData.GpsLong)
                    };
                    _gpsBuffer[gpsData.UnitNo] = gpsData;
                    _logger.LogInformation($"Received GPS data for unit {unitData.UnitNo}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Redis message");
            }
        });

        // Timer untuk broadcast
        _timer = new Timer(async _ =>
        {
            try
            {
                if (_gpsBuffer.IsEmpty)
                    return;

                var dataToSend = _gpsBuffer.Values.ToList();
                var json = JsonSerializer.Serialize(dataToSend);
                await WebSocketHandler.BroadcastAsync(json);
                _logger.LogInformation($"Broadcasted GPS data for {dataToSend.Count} units");

                // Clear buffer setelah berhasil dikirim
                _gpsBuffer.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending data to WebSocket clients");
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public override void Dispose()
    {
        base.Dispose();
        _timer?.Dispose();
    }
}

public class GpsData
{
    public string UnitNo { get; set; } = default!;
    public double Lat { get; set; }
    public double Lon { get; set; }
}

// Tambahkan class UnitData yang sama dengan yang ada di Worker.cs
public class UnitData
{
    [JsonPropertyName("unitno")]
    public string UnitNo { get; set; } = string.Empty;
    
    [JsonPropertyName("gpslat")]
    public string GpsLat { get; set; } = string.Empty;
    
    [JsonPropertyName("gpslong")]
    public string GpsLong { get; set; } = string.Empty;
    
    [JsonPropertyName("vehiclespeed")]
    public double VehicleSpeed { get; set; }
    
    [JsonPropertyName("geomaxspeed")]
    public string GeoMaxSpeed { get; set; } = "0";
    
    [JsonPropertyName("deviceid")]
    public string DeviceId { get; set; } = string.Empty;
    
    [JsonPropertyName("gpsspeed")]
    public double GpsSpeed { get; set; }
}

// WebSocketHandler untuk manajemen koneksi WebSocket
public static class WebSocketHandler
{
    private static readonly HashSet<WebSocket> _sockets = new();
    private static readonly object _lock = new();

    public static async Task HandleWebSocket(WebSocket socket)
    {
        lock (_lock)
        {
            _sockets.Add(socket);
        }
        var buffer = new byte[1024 * 4];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        finally
        {
            lock (_lock)
            {
                _sockets.Remove(socket);
            }
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the server", CancellationToken.None);
        }
    }

    public static async Task BroadcastAsync(string message)
    {
        var buffer = System.Text.Encoding.UTF8.GetBytes(message);
        List<WebSocket> toRemove = new();
        lock (_lock)
        {
            foreach (var socket in _sockets)
            {
                if (socket.State == WebSocketState.Open)
                {
                    _ = socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else
                {
                    toRemove.Add(socket);
                }
            }
            foreach (var s in toRemove)
                _sockets.Remove(s);
        }
    }
}
