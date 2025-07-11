using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReQueue.Interfaces;

namespace ReQueueTestProducer;

public class ProducerService(ILogger<ProducerService> logger, IConnectionHub hub, IHostApplicationLifetime appLifetime)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Producer service is running.");
        
        var queue = hub.GetMessageProducer("numQueue");
            
        int i = 0;
        while (stoppingToken.IsCancellationRequested == false)
        {
            await queue.PublishAsync(new Dictionary<string, string>()
            {
                { "orderId", Guid.NewGuid().ToString() },
                { "status", "created" },
                { "amount", i.ToString() }
            });
            
            i += 1;

            if (i == 10)
            {
                break;
            }

            await Task.Delay(1000, stoppingToken);
        }
        await queue.DeleteAsync();
        appLifetime.StopApplication();
    }
}