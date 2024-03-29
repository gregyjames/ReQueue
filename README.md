[![.NET](https://github.com/gregyjames/ReQueue/actions/workflows/dotnet.yml/badge.svg)](https://github.com/gregyjames/ReQueue/actions/workflows/dotnet.yml)
![NuGet Downloads](https://img.shields.io/nuget/dt/ReQueue)
![NuGet Version](https://img.shields.io/nuget/v/ReQueue)

# ReQueue
C# Library to use a redis list for asynchronous messaging. Perfect for small projects you don't want to set up RabbitMQ for. Try using it with a fast redis implemetation like [Dragonfly](https://github.com/dragonflydb/dragonfly).

# Example
## Object 
The library uses MessagePack under the hood, so make sure your object is marked as MessagePack serilizable. 
```csharp
[MessagePackObject(keyAsPropertyName: true)]
public class Data
{
    public int Foo { get; set; }
}
```

## Producer
```csharp
internal class Program
{
    static async Task Main(string[] args)
    {
        var manager = new ConnectionHub("localhost", 0);
        var queue = manager.GetMessageQueue<Data>("numQueue");

        for (int i = 0; i < 100; i++)
        {
            var item = new Data { Foo = i };
            await queue.EnqueueMessages(item);
            Console.WriteLine($"Sending -> {i}");
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
        var manager = new ConnectionHub("localhost", 0);
        var queue = manager.GetMessageQueue<Data>("numQueue");
        var tokenSource = new CancellationTokenSource();
        tokenSource.CancelAfter(TimeSpan.FromDays(6));

        await queue.DequeueMessages(x => {
            Console.WriteLine($"Recieved -> {x.Foo}");
        }, tokenSource.Token);
        Console.ReadLine();
    }
}
```
## Filter Example
Filters out messages that don't meet set criteria. By default, messages that are filtered out will be readded to the end of the queue.
```csharp
queue.DequeueMessages(x => {
    Console.WriteLine($"Recieved -> {x.Foo}");
}, tokenSource.Token, data => {
    if (data.Foo % 2 == 0){
        return true;
    }
    
    return false;
});
```
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
