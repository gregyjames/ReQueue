using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReQueue.Interfaces;
using StackExchange.Redis;

namespace ReQueue;

public delegate Task MessageReceivedHandler(ReQueueMessage message);
public class ReQueueConsumer: IConsumer
{
    private readonly IDatabase _db;
    private readonly string _redisKey;
    private readonly string _consumerGroup;
    private readonly string _consumerName;
    private readonly TimeSpan _keyPollInterval;
    private readonly Channel<ReQueueMessage> _channel;
    private CancellationTokenSource _cts;
    private readonly ILogger<ReQueueConsumer> _logger;

    public event MessageReceivedHandler? OnMessageReceived;
    public ReQueueConsumer(IDatabase db, string redisKey, string consumerGroup, string consumerName, TimeSpan keyPollInterval, int channelCapacity = 1000, ILogger<ReQueueConsumer>? logger = null)
    {
        _db = db;
        _redisKey = redisKey;
        _consumerGroup = consumerGroup;
        _consumerName = consumerName;
        _keyPollInterval = keyPollInterval;
        _cts = new CancellationTokenSource();
        _logger = logger ?? NullLogger<ReQueueConsumer>.Instance;
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
            _logger.LogDebug("Created consumer group: {consumerGroupName}", _consumerGroup);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Group already exists, ignore.
        }
    }
    
    public async Task StartConsuming(CancellationToken token = default)
    {
        _cts = new();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, token);
        _logger.LogInformation("Starting consumer {consumerName} in group {consumerGroupName}", _consumerName, _consumerGroup);
        var deletionTask = WatchForDeletionAsync(_redisKey, () =>
        {
            _logger.LogWarning("The queue {redisKey} has been deleted.", _redisKey);
            StopConsuming();
        }, _keyPollInterval);
        var readTask = ReadLoopAsync(_cts.Token);
        var processTask = ProcessLoopAsync(_cts.Token);
        await Task.WhenAll(readTask, processTask, deletionTask);
    }

    public void StopConsuming()
    {
        if (!_cts.IsCancellationRequested)
        {
            _logger.LogInformation("Stopping consumer {consumerName} in group {consumerGroupName}", _consumerName, _consumerGroup);
            _cts.Cancel();
        }
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
                    _logger.LogTrace("Received message: {messageId} in {consumerGroupName} on {consumerName}", message.Id, _consumerGroup, _consumerName);
                    await _channel.Writer.WriteAsync(message, token);
                }

                if (entries.Length == 0)
                    await Task.Delay(_keyPollInterval, token);
            }
        }
        catch (RedisServerException ex) when (ex.Message.Contains("NOGROUP"))
        {
            StopConsuming();
        }
        catch (TaskCanceledException ex)
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
            _logger.LogDebug("Message listener loop is cancelled.");
        }
    }
}