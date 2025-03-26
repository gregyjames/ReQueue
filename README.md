[![.NET](https://github.com/gregyjames/ReQueue/actions/workflows/dotnet.yml/badge.svg)](https://github.com/gregyjames/ReQueue/actions/workflows/dotnet.yml)
![NuGet Downloads](https://img.shields.io/nuget/dt/ReQueue)
![NuGet Version](https://img.shields.io/nuget/v/ReQueue)

# ReQueue
C# Library to use a redis streams for asynchronous messaging. Perfect for small projects you don't want to set up RabbitMQ for. Try using it with a fast redis implemetation like [Dragonfly](https://github.com/dragonflydb/dragonfly).

# Example
## Producer
```csharp
internal class Program
    {
        static async Task Main(string[] args)
        {
            var manager = new ConnectionHub("localhost:6379", 0);
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
            }
            Console.ReadLine();
        }
    }
```

## Consumer
```csharp
internal class Program
    {
        static async Task Main(string[] args)
        {
            var manager = new ConnectionHub("localhost:6379", 0);
            var consumer = manager.GetMessageConsumer("numQueue", "order-group", "order-processor-1");

            consumer.OnMessageReceived += ConsumerOnOnMessageReceived;
            consumer.StartConsuming();
            
            Console.ReadLine();
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
```
## Features
- Non-blocking asynchronous processing
- Delegate-based event handling (`OnMessageReceived`)
- Simple start/stop consumer API
- Generic message payload support
- Efficient resource usage

# License
MIT License

Copyright (c) 2024 Greg James

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
