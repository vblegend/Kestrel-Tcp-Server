
using KestrelServer.Message;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace KestrelServer
{



    public class Program
    {
        private static CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        public static async Task Main(string[] args)
        {
            var host = CreateHost(args);
            await host.RunAsync();
            host.Dispose();
            Console.ReadLine();
        }






        public static IHost CreateHost(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            ConfigureLifetime(host);
            return host;
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                 .UseSerilog(ConfigureSerilog)
                 .ConfigureLogging(ConfigureLogging)
                 .ConfigureServices(ConfigureServices);
        }

        public static void ConfigureLifetime(IHost host)
        {
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            lifetime.ApplicationStarted.Register(() =>
            {
                logger.LogInformation("Started..");
            });
            lifetime.ApplicationStopping.Register(() =>
            {
                logger.LogInformation("Shutting Down..");
                //cancellationSource.Cancel(true);
            });

            lifetime.ApplicationStopped.Register(() =>
            {
                logger.LogInformation("Stopped..");
            });
        }


        private static void ConfigureLogging(ILoggingBuilder logging)
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Debug);
        }

        private static void ConfigureSerilog(HostBuilderContext context, LoggerConfiguration configuration)
        {
            configuration.MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug);
            configuration.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning);
            configuration.WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u4}] {Message:lj}{NewLine}{Exception}");//  [{SourceContext}]
            configuration.Enrich.FromLogContext();
        }


        private static void ConfigureServices(IServiceCollection services)
        {
            var ipBlock = new IPBlacklistTrie();
            //ipBlock.Add("127.0.0.1");
            //ipBlock.Add("192.168.1.1/24");

            services.AddSingleton<IPBlacklistTrie>(ipBlock);
            services.AddSingleton<GMPayloadResolver>();
            services.AddSingleton<GMessageParser>();
            services.AddSingleton<TestService>();
            services.AddHostedService(provider => provider.GetRequiredService<TestService>());
            services.AddTimeService();
            services.AddSingleton<TCPConnectionHandler>();
            services.AddHostedService(provider => provider.GetRequiredService<TCPConnectionHandler>());
        }

    }
}
