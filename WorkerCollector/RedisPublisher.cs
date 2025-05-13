using StackExchange.Redis;
using System.Text.Json;

public class RedisGpsPublisher : IGpsPublisher
{
    private readonly IConnectionMultiplexer _redis;

    public RedisGpsPublisher(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task PublishAsync(GpsData data)
    {
        var publisher = _redis.GetSubscriber();
        var json = JsonSerializer.Serialize(data);
        await publisher.PublishAsync("gps_update", json);
    }
}
