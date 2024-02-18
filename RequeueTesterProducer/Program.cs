
namespace RequeueTesterProducer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var queue = new ReQueue.MessageQueue<Data>("localhost", 0);

            for (int i = 0; i < 1000; i++)
            {
                var item = new Data { Foo = i };
                await queue.EnqueueMessages("numQueue", item);
                Console.WriteLine($"Sending -> {i}");
            }

            Console.ReadLine();
        }
    }
}
