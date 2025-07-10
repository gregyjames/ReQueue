using StackExchange.Redis;

namespace ReQueue
{
    public class ReQueueProducer
    {
        private readonly IDatabase _db;
        private readonly string _redisKey;

        /// <summary>
        /// Create a new client object.
        /// </summary>
        /// <param name="database">The redis database to use.</param>
        /// <param name="redisKey">The name of the redis list.</param>
        internal ReQueueProducer(IDatabase database, string redisKey)
        {
            _db = database;
            _redisKey = redisKey;
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

            return messageId;
        }

        public async Task DeleteAsync()
        {
            var groups = _db.StreamGroupInfo(_redisKey);

            foreach (var group in groups)
            {
                await _db.StreamDeleteConsumerGroupAsync(_redisKey, group.Name);
            }
            await _db.KeyDeleteAsync(_redisKey);
        }
    }
}
