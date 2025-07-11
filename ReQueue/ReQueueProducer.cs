using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace ReQueue
{
    public class ReQueueProducer: IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _redisKey;
        private readonly bool _autoDeleteOnExit;
        private readonly ILogger<ReQueueProducer> _logger;

        /// <summary>
        /// Create a new client object.
        /// </summary>
        /// <param name="database">The redis database to use.</param>
        /// <param name="redisKey">The name of the redis list.</param>
        /// <param name="autoDeleteOnExit">Delete the queue when disposed.</param>
        internal ReQueueProducer(IDatabase database, string redisKey, bool autoDeleteOnExit = false, ILogger<ReQueueProducer> logger = null)
        {
            _logger = logger ?? NullLogger<ReQueueProducer>.Instance;
            _db = database;
            _redisKey = redisKey;
            _autoDeleteOnExit = autoDeleteOnExit;
        }

        public async Task<string> PublishAsync(Dictionary<string, string> message)
        {
            var entries = new NameValueEntry[message.Count];
            
            var i = 0;

            foreach (var kv in message)
            {
                entries[i++] =  new NameValueEntry(kv.Key, kv.Value);
            }

            var messageId = await _db.StreamAddAsync(_redisKey, entries);
            _logger.LogTrace($"Published new message with id {messageId}");
            
            return messageId;
        }

        public async Task DeleteAsync()
        {
            var groups = _db.StreamGroupInfo(_redisKey);

            foreach (var group in groups)
            {
                _logger.LogTrace("Removed consumer group {consumerGroup} from key {queueName}", group.Name, _redisKey);
                await _db.StreamDeleteConsumerGroupAsync(_redisKey, group.Name);
            }
            await _db.KeyDeleteAsync(_redisKey);
            
            _logger.LogInformation("Deleted queue with name {queueName}", _redisKey);
        }

        public async ValueTask DisposeAsync()
        {
            if (_autoDeleteOnExit)
            {
                await DeleteAsync();
            }
        }
    }
}
