public interface IGpsPublisher
{
    Task PublishAsync(GpsData data);
}
