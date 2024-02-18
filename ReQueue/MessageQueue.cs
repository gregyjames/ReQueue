using MessagePack;
using StackExchange.Redis;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ReQueue
{
    public class MessageQueue<T>
    {
        private ConnectionMultiplexer connectionMultiplexer = null;
        private IDatabase db = null;

        public MessageQueue(string connectionString, int dbnumber = 0)
        {
            connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            db = connectionMultiplexer.GetDatabase(dbnumber);
        }

        public async Task EnqueueMessages(string queueKey, T item)
        {
            byte[] bytes = MessagePackSerializer.Serialize(item);
            string base64String = Convert.ToBase64String(bytes);
            await db.ListLeftPushAsync(queueKey, base64String);
        }

        public async Task DequeueMessages(string queueKey, Action<T> action, CancellationToken cancellationToken)
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
                        action.Invoke(deserialized);
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., connection issues)
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
