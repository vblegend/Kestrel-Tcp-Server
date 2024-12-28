
using KestrelServer.Message;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KestrelServer
{



    public class Program
    {
        private static CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        public static async Task Main(string[] args)
        {

            var host = CreateHostBuilder(args).Build();
            host.UseCtrlCCancel(_cancellationSource);
            await host.RunAsync(_cancellationSource.Token);
            host.Dispose();
            Console.ReadLine();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                 .ConfigureLogging(ConfigureLogging)
                 .ConfigureServices(ConfigureServices);
        }


        private static void ConfigureLogging(ILoggingBuilder logging)
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Information);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var ipBlock = new IPBlacklistTrie();
            //ipBlock.Add("127.0.0.1");
            //ipBlock.Add("192.168.1.1/24");


            services.AddSerilog((services, loggerConfiguration) =>
            {
                loggerConfiguration.ReadFrom.Services(services);
                loggerConfiguration.Enrich.FromLogContext();
                loggerConfiguration.WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u4}] {Message:lj}{NewLine}{Exception}");
            });


            services.AddSingleton<IPBlacklistTrie>(ipBlock);

            services.AddSingleton<GMPayloadResolver>();
            services.AddSingleton<GMessageParser>();


            services.AddSingleton<TestService>();
            services.AddHostedService(provider => provider.GetRequiredService<TestService>());



            services.AddSingleton<UTCTimeService>();
            services.AddHostedService(provider => provider.GetRequiredService<UTCTimeService>());



            services.AddSingleton<TCPConnectionHandler>();
            services.AddHostedService(provider => provider.GetRequiredService<TCPConnectionHandler>());


        }

    }
}
