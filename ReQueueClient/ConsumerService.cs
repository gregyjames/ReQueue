using Microsoft.Extensions.Hosting;
using ReQueue.Interfaces;

namespace ReQueueClient;

public class ConsumerService: BackgroundService
{
    private readonly IConnectionHub _hub;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public ConsumerService(IConnectionHub hub, IHostApplicationLifetime applicationLifetime)
    {
        _hub = hub;
        _applicationLifetime = applicationLifetime;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = _hub.GetMessageConsumer("numQueue", $"order-group-{Guid.NewGuid():N}", "order-processor-1", TimeSpan.FromSeconds(1));
            
        await consumer.StartConsuming(stoppingToken);
        
        await consumer.WatchForDeletionAsync("numQueue", () =>
        {
            _applicationLifetime.StopApplication();
        });
    }
}