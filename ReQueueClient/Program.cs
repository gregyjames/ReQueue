using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReQueue;
using ReQueue.Extensions;
using Serilog;
using ILogger = Serilog.ILogger;

namespace ReQueueClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await Host
                .CreateDefaultBuilder(args)
                .UseSerilog((context, configuration) =>
                {
                    configuration.MinimumLevel.Verbose().WriteTo.Console();
                })
                .ConfigureAppConfiguration(cfg => cfg.AddJsonFile("appsettings.json"))
                .ConfigureServices(cfg =>
                {
                    cfg.AddHostedService<ConsumerService>();
                })
                .AddReQueue()
                .RunConsoleAsync();
        }
    }
}
