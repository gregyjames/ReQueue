using Microsoft.Extensions.Logging;
using ReQueue;
using Serilog;
using ILogger = Serilog.ILogger;

namespace ReQueueClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();
            
            var factory = new LoggerFactory().AddSerilog(configuration); 
            var manager = new ConnectionHub("192.168.0.117:6379", 0, factory);
            var queue = manager.GetMessageQueue("numQueue");
            
            int i = 0;
            while (true)
            {
                var id = await queue.PublishAsync(new Dictionary<string, string>()
                {
                    { "orderId", Guid.NewGuid().ToString() },
                    { "status", "created" },
                    { "amount", i.ToString() }
                });
                i += 1;

                if (i == 100)
                {
                    break;
                }
                Thread.Sleep(50);
            }
            await queue.DeleteAsync();
        }
    }
}
