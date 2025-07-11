using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReQueue.Interfaces;

namespace RequeueTesterProducer;

public class ProducerService: BackgroundService
{
    private readonly ILogger<ProducerService> _logger;
    private readonly IConnectionHub _hub;
    private readonly IHostApplicationLifetime _appLifetime;

    public ProducerService(ILogger<ProducerService> logger, IConnectionHub hub, IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _hub = hub;
        _appLifetime = appLifetime;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queue = _hub.GetMessageProducer("numQueue");
            
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
        _appLifetime.StopApplication();
    }
}