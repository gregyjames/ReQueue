
using ReQueue;

namespace RequeueTesterProducer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var manager = new ConnectionHub("localhost", 0);
            var queue = manager.GetMessageQueue<Data>("numQueue");

            for (int i = 0; i < 1000; i++)
            {
                var item = new Data { Foo = i };
                await queue.EnqueueMessages(item);
                Console.WriteLine($"Sending -> {i}");
            }

            Console.ReadLine();
        }
    }
}
