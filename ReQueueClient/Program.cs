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
            var factory = new LoggerFactory().AddSerilog(new LoggerConfiguration().WriteTo.Console().CreateLogger());
            var logger = factory.CreateLogger<ReQueueProducer>();   
            var manager = new ConnectionHub("192.168.0.117:6379", 0);
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
                Console.WriteLine(id);
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
