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
    private readonly TimeSpan _keyPollInterval;
    private readonly Channel<ReQueueMessage> _channel;
    private CancellationTokenSource _cts;
    
    public event MessageReceivedHandler OnMessageReceived;
    public ReQueueConsumer(IDatabase db, string redisKey, string consumerGroup, string consumerName, TimeSpan keyPollInterval, int channelCapacity = 1000)
    {
        _db = db;
        _redisKey = redisKey;
        _consumerGroup = consumerGroup;
        _consumerName = consumerName;
        _keyPollInterval = keyPollInterval;
        _cts = new CancellationTokenSource();
        
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
    
    public async Task StartConsuming(CancellationToken token = default)
    {
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, token);
        var readTask = ReadLoopAsync(linkedToken.Token);
        var processTask = ProcessLoopAsync(linkedToken.Token);
        var deletionTask = WatchForDeletionAsync(_redisKey, () =>
        {
            _cts.Cancel();
        }, _keyPollInterval);
        await Task.WhenAll(readTask, processTask,deletionTask);
    }

    public void StopConsuming()
    {
        _cts.Cancel();
    }

    public async Task WatchForDeletionAsync(string key, Action onDeleted, TimeSpan pollInterval = default)
    {
        pollInterval = pollInterval == default ? TimeSpan.FromSeconds(1) : pollInterval;
    
        while (await _db.KeyExistsAsync(key))
        {
            await Task.Delay(pollInterval);
        }
    
        onDeleted?.Invoke();
    }
    
    private async Task ReadLoopAsync(CancellationToken token)
    {
        try
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
        }
        catch (RedisServerException ex) when (ex.Message.Contains("NOGROUP"))
        {
            StopConsuming();
        }
        finally
        {
            _channel.Writer.Complete();
        }
    }

    private async Task ProcessLoopAsync(CancellationToken token)
    {
        try
        {
            await foreach (var message in _channel.Reader.ReadAllAsync(token))
            {
                if (OnMessageReceived != null)
                    await OnMessageReceived.Invoke(message);

                await _db.StreamAcknowledgeAsync(_redisKey, _consumerGroup, message.Id);
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
        {

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