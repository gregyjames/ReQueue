
using ReQueue;

namespace RequeueTesterProducer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var manager = new ConnectionHub("192.168.0.117:6379", 0);
            var consumer = manager.GetMessageConsumer("numQueue", $"order-group-{Guid.NewGuid():N}", "order-processor-1", TimeSpan.FromSeconds(1));

            consumer.OnMessageReceived += ConsumerOnOnMessageReceived;
            await consumer.StartConsuming();
            
            consumer.StopConsuming();
        }

        private static async Task ConsumerOnOnMessageReceived(ReQueueMessage message)
        {
            Console.WriteLine($"Received message: {message.Id}");
            foreach (var kv in message.Values)
                Console.WriteLine($" - {kv.Key}: {kv.Value}");

            await Task.CompletedTask;
        }
    }
}
