using StackExchange.Redis;

namespace ReQueue;

public class ConnectionHub
{
    private readonly ConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _db;
    
    /// <summary>
    /// Initialize a new connection hub instance.
    /// </summary>
    /// <param name="connectionString">The connection string of the redis database to use.</param>
    /// <param name="dbnumber">The number of the database to use.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ConnectionHub(string connectionString, int dbnumber = 0)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }
        _connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
        _db = _connectionMultiplexer.GetDatabase(dbnumber);
    }

    /// <summary>
    /// Creates a new queue of the specified type.
    /// </summary>
    /// <param name="redisKey">The key of the list in redis.</param>
    /// <typeparam name="T">The type of the queue to create.</typeparam>
    /// <returns>A message queue object of specified type.</returns>
    public MessageQueue<T> GetMessageQueue<T>(string redisKey)
    {
        return new MessageQueue<T>(_db, redisKey);
    }
}