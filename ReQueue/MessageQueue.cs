using MessagePack;
using StackExchange.Redis;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ReQueue
{
    public class MessageQueue<T>
    {
        private ConnectionMultiplexer connectionMultiplexer = null;
        private IDatabase db = null;

        /// <summary>
        /// Create a new MessageQueue object.
        /// </summary>
        /// <param name="connectionString">The redis connection string.</param>
        /// <param name="dbnumber">The number of the database to use.</param>
        public MessageQueue(string connectionString, int dbnumber = 0)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }
            connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            db = connectionMultiplexer.GetDatabase(dbnumber);
        }

        /// <summary>
        /// Adds the item to the message queue.
        /// </summary>
        /// <param name="queueKey">The name of the queue.</param>
        /// <param name="item">The item to be added.</param>
        /// <returns></returns>
        public async Task EnqueueMessages(string queueKey, T item)
        {
            byte[] bytes = MessagePackSerializer.Serialize(item);
            string base64String = Convert.ToBase64String(bytes);
            await db.ListLeftPushAsync(queueKey, base64String);
        }

        /// <summary>
        /// Task to wait and recieve all messages from the queue.
        /// </summary>
        /// <param name="queueKey">The name of the queue.</param>
        /// <param name="action">The action to be run when an item is recieved.</param>
        /// <param name="cancellationToken">The token to be used for cancelling the task.</param>
        /// <param name="filter">Optional filter for recieved messages. By default, items that fail the filter are requeued.</param>
        /// <returns></returns>
        public async Task DequeueMessages(string queueKey, Action<T> action, CancellationToken cancellationToken, Func<T, bool> filter = null)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Use BLPOP to block and wait for an item to be added to the list
                    var result = await db.ListRightPopAsync(queueKey); 

                    if (result != RedisValue.Null)
                    {
                        // Process the item
                        string item = result.ToString();
                        byte[] decodedData = Convert.FromBase64String(item);
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
                                await EnqueueMessages(queueKey, deserialized);
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
        /// <param name="queueKey">The name of the queue.</param>
        /// <returns></returns>
        public async Task ClearQueue(string queueKey)
        {
            await db.KeyDeleteAsync(queueKey);
        }
    }
}
