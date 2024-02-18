using ReQueue;

namespace ReQueueClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var queue = new MessageQueue<Data>("localhost", 0);
            var tokenSource = new CancellationTokenSource();

            tokenSource.CancelAfter(TimeSpan.FromSeconds(10));

            await Task.WhenAll(
                queue.DequeueMessages("numQueue", new Action<Data>(x => {
                    Console.WriteLine($"Recieved -> {x.Foo}");
                }), tokenSource.Token, new Func<Data, bool>((data) => {
                    if (data != null)
                    {
                        if (data.Foo % 2 == 0)
                        {
                            return true;
                        }
                    }

                    return false;
                })),
                queue.DequeueMessages("numQueue", new Action<Data>(x => {
                    Console.WriteLine($"Recieved Odd -> {x.Foo}");
                }), tokenSource.Token, new Func<Data, bool>((data) => {
                    if (data != null){
                        if (data.Foo % 2 != 0){
                            return true;
                        }
                    }

                    return false;
                }))
            );

            await queue.ClearQueue("numQueue");
            Console.ReadLine();
        }
    }
}
