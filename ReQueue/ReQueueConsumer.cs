using System.Threading.Channels;
using StackExchange.Redis;

namespace ReQueue;

public delegate Task MessageReceivedHandler(ReQueueMessage message);
public class ReQueueConsumer
{
    private readonly IDatabase _db;
    private readonly string _redisKey;
    private readonly string _consumerGroup;
    private readonly string _consumerName;
    private readonly Channel<ReQueueMessage> _channel;
    private CancellationTokenSource _cts;
    
    public event MessageReceivedHandler OnMessageReceived;
    public ReQueueConsumer(IDatabase db, string redisKey, string consumerGroup, string consumerName, int channelCapacity = 1000)
    {
        _db = db;
        _redisKey = redisKey;
        _consumerGroup = consumerGroup;
        _consumerName = consumerName;
        
        _channel = Channel.CreateBounded<ReQueueMessage>(new BoundedChannelOptions(channelCapacity)
        {
            SingleReader = true,
            SingleWriter = true
        });
        
        CreateConsumerGroupIfNotExistsAsync().Wait();

    }
    
    private async Task CreateConsumerGroupIfNotExistsAsync()
    {
        try
        {
            await _db.StreamCreateConsumerGroupAsync(_redisKey, _consumerGroup, StreamPosition.NewMessages);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Group already exists, ignore.
        }
    }
    
    public void StartConsuming()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ReadLoopAsync(_cts.Token));
        _ = Task.Run(() => ProcessLoopAsync(_cts.Token));
    }

    public void StopConsuming()
    {
        _cts.Cancel();
    }

    private async Task ReadLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var entries = await _db.StreamReadGroupAsync(_redisKey, _consumerGroup, _consumerName, ">", count: 10);

            foreach (var entry in entries)
            {
                var message = ReQueueMessage.FromStreamEntry(entry);
                await _channel.Writer.WriteAsync(message, token);
            }

            if (entries.Length == 0)
                await Task.Delay(100, token);
        }

        _channel.Writer.Complete();
    }

    private async Task ProcessLoopAsync(CancellationToken token)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(token))
        {
            if (OnMessageReceived != null)
                await OnMessageReceived.Invoke(message);

            await _db.StreamAcknowledgeAsync(_redisKey, _consumerGroup, message.Id);
        }
    }
    
    public async Task<List<ReQueueMessage>> ConsumeAsync(int count = 10)
    {
        var result = new List<ReQueueMessage>();
        var entries = await _db.StreamReadGroupAsync(_redisKey, _consumerGroup, _consumerName, ">", count);

        foreach (var entry in entries)
        {
            result.Add(ReQueueMessage.FromStreamEntry(entry));
            await _db.StreamAcknowledgeAsync(_redisKey, _consumerGroup, entry.Id);
        }

        return result;
    }
    
    public async Task<List<ReQueueMessage>> ConsumePendingAsync(int count = 10)
    {
        var result = new List<ReQueueMessage>();
        var entries = await _db.StreamReadGroupAsync(_redisKey, _consumerGroup, _consumerName, "0-0", count);

        foreach (var entry in entries)
            result.Add(ReQueueMessage.FromStreamEntry(entry));

        return result;
    }
}