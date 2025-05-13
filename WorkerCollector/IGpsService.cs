using System.Collections.Concurrent;

namespace WorkerCollector;

public interface IGpsService
{
    ConcurrentDictionary<string, GpsData> GpsBuffer { get; }
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
} 