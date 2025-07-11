using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReQueue.Interfaces;

namespace ReQueueTestConsumer;

public class ConsumerService(ILogger<ConsumerService> logger, IConnectionHub hub, IHostApplicationLifetime applicationLifetime)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Consumer Service is running.");
        
        var consumer = hub.GetMessageConsumer("numQueue", $"order-group-{Guid.NewGuid():N}", "order-processor-1", TimeSpan.FromSeconds(1));
            
        await consumer.StartConsuming(stoppingToken);
        
        await consumer.WatchForDeletionAsync("numQueue", () =>
        {
            applicationLifetime.StopApplication();
        });
    }
}