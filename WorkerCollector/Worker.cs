using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkerCollector
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ConnectionMultiplexer _redis;
        private ClientWebSocket _webSocket;
        private int _counter = 1;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _redis = ConnectionMultiplexer.Connect("localhost:6381");  // Connect ke Redis
            _webSocket = new ClientWebSocket();  // WebSocket client
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _webSocket.ConnectAsync(new Uri("ws://localhost:8767"), stoppingToken);  // WebSocket URL

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var buffer = new byte[1024 * 4];  // Buffer untuk menerima pesan
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
                    

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var jsonString = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogInformation($"Raw data received: {jsonString}");
                        
                        // Deserialize JSON menjadi objek UnitData
                        try 
                        {
                            var unitData = JsonSerializer.Deserialize<UnitData>(jsonString);
                            _logger.LogInformation($"Deserialized data: UnitNo={unitData?.UnitNo}, GpsLat={unitData?.GpsLat}, GpsLong={unitData?.GpsLong}");

                            if (unitData != null)
                            {
                                // Simpan ke Redis
                                var db = _redis.GetDatabase();
                                var redisKey = $"unit:{unitData.UnitNo}";

                                var hashEntries = new HashEntry[]
                                {
                                    new HashEntry("unitno", unitData.UnitNo),
                                    new HashEntry("gpslat", unitData.GpsLat)    ,
                                    new HashEntry("gpslong", unitData.GpsLong),
                                    new HashEntry("vehiclespeed", unitData.VehicleSpeed),
                                    new HashEntry("geomaxspeed", unitData.GeoMaxSpeed),
                                    new HashEntry("deviceid", unitData.DeviceId),
                                    new HashEntry("gpsspeed", unitData.GpsSpeed),
                                };

                                // Menyimpan data ke Redis
                                await db.HashSetAsync(redisKey, hashEntries);
                                var pub = _redis.GetSubscriber();
                                await pub.PublishAsync("gps_update", jsonString);


                                _logger.LogInformation($"Unit {unitData.UnitNo} data saved to Redis.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error while deserializing data");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while receiving data from WebSocket");
                    await Task.Delay(1000, stoppingToken);  // Coba lagi setelah 1 detik jika terjadi error
                }
            }
        }

        public override void Dispose()
        {
            _webSocket?.Dispose();
            _redis?.Dispose();
            base.Dispose();
        }
    }

    // Contoh model UnitData yang mewakili data JSON yang diterima
    public class UnitData{
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
}
