
using Microsoft.Extensions.Logging;
using ReQueue;
using Serilog;
using Serilog.Events;

namespace RequeueTesterProducer
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
            var manager = new ConnectionHub(options =>
            {
                options.ConnectionString = "192.168.0.117:6379";
                options.DB = 0;
            }, factory);
            
            var consumer = manager.GetMessageConsumer("numQueue", $"order-group-{Guid.NewGuid():N}", "order-processor-1", TimeSpan.FromSeconds(1));

            consumer.OnMessageReceived += ConsumerOnOnMessageReceived;
            await consumer.StartConsuming();
            
            consumer.StopConsuming();
        }

        private static async Task ConsumerOnOnMessageReceived(ReQueueMessage message)
        {
            //Console.WriteLine($"Received message: {message.Id}");
            //foreach (var kv in message.Values)
                //Console.WriteLine($" - {kv.Key}: {kv.Value}");

            await Task.CompletedTask;
        }
    }
}
