using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReQueue.Interfaces;
using StackExchange.Redis;

namespace ReQueue.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder AddReQueue(this IHostBuilder hostBuilder, Action<RedisOptions> configureOptions)
    {
        return hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddSingleton<IConnectionHub>(new ConnectionHub(configureOptions));
        });
    }
    
    public static IHostBuilder AddReQueue(this IHostBuilder hostBuilder, IDatabase database)
    {
        return hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddSingleton<IConnectionHub, ConnectionHub>(provider =>
            {
                var loggerFactory = provider.GetService<ILoggerFactory>() ?? null;
                
                return new ConnectionHub(database, loggerFactory);
            });
        });
    }
    
    public static IHostBuilder AddReQueue(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices((context, services) =>
        {
            services.Configure<RedisOptions>(context.Configuration.GetSection("RedisOptions"));
            services.AddSingleton<IConnectionHub, ConnectionHub>();
        });
    }
}