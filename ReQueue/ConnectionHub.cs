using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace ReQueue;

public class ConnectionHub
{
    private readonly IDatabase _db;
    private readonly ILoggerFactory _factory;

    /// <summary>
    /// Initialize a new connection hub instance.
    /// </summary>
    /// <param name="connectionString">The connection string of the redis database to use.</param>
    /// <param name="dbnumber">The number of the database to use.</param>
    /// <param name="loggerFactory">The Logging factory to use</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ConnectionHub(string connectionString, int dbnumber = 0, ILoggerFactory? loggerFactory = null)
    {
        _factory = loggerFactory ?? NullLoggerFactory.Instance;
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }
        var connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
        _db = connectionMultiplexer.GetDatabase(dbnumber);
    }

    /// <summary>
    /// Initialize a new connection hub instance.
    /// </summary>
    /// <param name="db">The redis database to use.</param>
    /// <exception cref="ArgumentNullException">Thrown if the passed in database is null.</exception>
    public ConnectionHub(IDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Creates a new queue of the specified type.
    /// </summary>
    /// <param name="redisKey">The key of the list in redis.</param>
    /// <param name="autoDelete">Delete the queue when disposed.</param>
    /// <returns>A message queue object of specified type.</returns>
    public ReQueueProducer GetMessageQueue(string redisKey, bool autoDelete = false)
    {
        return new ReQueueProducer(_db, redisKey, autoDelete);
    }

    public ReQueueConsumer GetMessageConsumer(string redisKey, string consumerGroup, string consumerName,  TimeSpan pollInterval, int channelCapacity = 10000)
    {
        var logger = _factory.CreateLogger<ReQueueConsumer>();
        return new ReQueueConsumer(_db, redisKey,consumerGroup, consumerName, pollInterval, channelCapacity, logger);
    }
}