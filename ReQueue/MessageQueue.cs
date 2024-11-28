using MessagePack;
using StackExchange.Redis;

namespace ReQueue
{
    public class MessageQueue<T>
    {
        private readonly IDatabase _db;
        private readonly string _redisKey;

        /// <summary>
        /// Create a new MessageQueue object.
        /// </summary>
        /// <param name="database">The redis database to use.</param>
        /// <param name="redisKey">The name of the redis list.</param>
        internal MessageQueue(IDatabase database, string redisKey)
        {
            _db = database;
            _redisKey = redisKey;
        }

        /// <summary>
        /// Adds the item to the message queue.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        /// <returns></returns>
        public async Task EnqueueMessages(T item)
        {
            var bytes = MessagePackSerializer.Serialize(item);
            var base64String = Convert.ToBase64String(bytes);
            await _db.ListLeftPushAsync(_redisKey, base64String);
        }

        /// <summary>
        /// Task to wait and receive all messages from the queue.
        /// </summary>
        /// <param name="action">The action to be run when an item is received.</param>
        /// <param name="cancellationToken">The token to be used for cancelling the task.</param>
        /// <param name="filter">Optional filter for received messages. By default, items that fail the filter are re-queued.</param>
        /// <returns></returns>
        public async Task DequeueMessages(Action<T> action, CancellationToken cancellationToken, Func<T, bool> filter = null!)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Use BLPOP to block and wait for an item to be added to the list
                    var result = await _db.ListRightPopAsync(_redisKey); 

                    if (result != RedisValue.Null)
                    {
                        // Process the item
                        var item = result.ToString();
                        var decodedData = Convert.FromBase64String(item);
                        var deserialized = MessagePackSerializer.Deserialize<T>(decodedData);

                        if(filter != null)
                        {
                            var passed = filter.Invoke(deserialized);
                            if (passed)
                            {
                                action.Invoke(deserialized);
                            }
                            else
                            {
                                await EnqueueMessages(deserialized);
                            }
                        }
                        else
                        {
                            action.Invoke(deserialized);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., connection issues)
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clears all unprocessed items from the queue.
        /// </summary>
        /// <returns></returns>
        public async Task ClearQueue()
        {
            await _db.KeyDeleteAsync(_redisKey);
        }
    }
}
