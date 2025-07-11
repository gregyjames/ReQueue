using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReQueue.Interfaces;
using StackExchange.Redis;

namespace ReQueue;

public class ConnectionHub: IConnectionHub
{
    private readonly IDatabase _db;
    private readonly ILoggerFactory _factory;
    private readonly RedisOptions _options;

    /// <summary>
    /// Initialize a new connection hub instance.
    /// </summary>
    /// <param name="options">Options for redis configuration.</param>
    /// <param name="loggerFactory">The Logging factory to use</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ConnectionHub(IOptions<RedisOptions> options, ILoggerFactory? loggerFactory = null)
    {
        _factory = loggerFactory ?? NullLoggerFactory.Instance;
        _options = options.Value;
        
        //todo: validate options
        
        var connectionMultiplexer = ConnectionMultiplexer.Connect(_options.ConnectionString, options =>
        {
            options.User = _options.Username;
            options.Password = _options.Password;
            options.Ssl = _options.UseSsl;
        });
        
        _db = connectionMultiplexer.GetDatabase(_options.DB);
    }

    /// <summary>
    /// Initialize a new connection hub instance.
    /// </summary>
    /// <param name="options">The method used to configure the Redis instance.</param>
    /// <param name="loggerFactory">The Logging factory to use.</param>
    public ConnectionHub(Action<RedisOptions> options, ILoggerFactory? loggerFactory = null)
    {
        _factory = loggerFactory ?? NullLoggerFactory.Instance;
        _options = new RedisOptions();
        options.Invoke(_options);
        
        //todo: validate options
        
        var connectionMultiplexer = ConnectionMultiplexer.Connect(_options.ConnectionString, options =>
        {
            options.User = _options.Username;
            options.Password = _options.Password;
            options.Ssl = _options.UseSsl;
        });
        
        _db = connectionMultiplexer.GetDatabase(_options.DB);
    }

    /// <summary>
    /// Initialize a new connection hub instance.
    /// </summary>
    /// <param name="db">The redis database to use.</param>
    /// <param name="loggerFactory">The Logging factory to use.</param>
    /// <exception cref="ArgumentNullException">Thrown if the passed in database is null.</exception>
    public ConnectionHub(IDatabase db, ILoggerFactory? loggerFactory = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _factory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    /// <summary>
    /// Creates a new queue of the specified type.
    /// </summary>
    /// <param name="redisKey">The key of the list in redis.</param>
    /// <param name="autoDelete">Delete the queue when disposed.</param>
    /// <returns>A message queue object of specified type.</returns>
    public IProducer GetMessageProducer(string redisKey, bool autoDelete = false)
    {
        var logger = _factory.CreateLogger<ReQueueProducer>();
        return new ReQueueProducer(_db, redisKey, autoDelete, logger);
    }

    public IConsumer GetMessageConsumer(string redisKey, string consumerGroup, string consumerName,  TimeSpan pollInterval, int channelCapacity = 10000)
    {
        var logger = _factory.CreateLogger<ReQueueConsumer>();
        return new ReQueueConsumer(_db, redisKey,consumerGroup, consumerName, pollInterval, channelCapacity, logger);
    }
}