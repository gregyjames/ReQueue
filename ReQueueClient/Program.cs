using ReQueue;

namespace ReQueueClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var queue = new ReQueue.MessageQueue<Data>("localhost", 0);
            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromDays(6));

            await queue.DequeueMessages("numQueue", new Action<Data>(x => {
                Console.WriteLine($"Recieved -> {x.Foo}");
            }), tokenSource.Token);
            Console.ReadLine();
        }
    }
}
