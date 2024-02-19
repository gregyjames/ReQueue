using ReQueue;

namespace ReQueueClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var manager = new ConnectionHub("localhost", 0);
            var queue = manager.GetMessageQueue<Data>("numQueue");
            var tokenSource = new CancellationTokenSource();

            tokenSource.CancelAfter(TimeSpan.FromSeconds(10));

            await Task.WhenAll(
                queue.DequeueMessages(x => {
                    Console.WriteLine($"Recieved -> {x.Foo}");
                }, tokenSource.Token, (data) => data.Foo % 2 == 0),
                queue.DequeueMessages(x => {
                    Console.WriteLine($"Recieved Odd -> {x.Foo}");
                }, tokenSource.Token, (data) => data.Foo % 2 != 0)
            );

            await queue.ClearQueue();
            Console.ReadLine();
        }
    }
}
