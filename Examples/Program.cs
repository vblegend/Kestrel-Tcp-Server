
using Examples;
using Examples.Client;
using Examples.Services;
using LightNet.Message;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

namespace LightNet
{



    public class Program
    {

        private static readonly Logger logger = InitSerilog();

        private static Logger InitSerilog()
        {
            var configuration = new LoggerConfiguration();
            configuration.MinimumLevel.Is(Serilog.Events.LogEventLevel.Information);
            configuration.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning);
            configuration.WriteTo.Async(configure =>
            {
                configure.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss fff} [{Level:u4}] {Message:lj}{NewLine}{Exception}");//  [{SourceContext}]
            });
            configuration.Enrich.FromLogContext();
            var _logger = configuration.CreateLogger();
            ILoggerFactory loggerFactory = new SerilogLoggerFactory(_logger);
            LoggerProvider.Initialize(loggerFactory);
            return _logger;
        }


        public static async Task Main(string[] args)
        {
            // 动态调整 内存池容量
            MFactory<ClientMessage>.SetPoolMaxCapacity(1500000, true);
            // 动态调整 禁用内存池
            MFactory<ClientMessage>.SetPoolMaxCapacity(0, true);


            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
            host.Dispose();
            Console.ReadLine();
        }


        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                 .UseSerilog(logger)
                 .ConfigureServices(ConfigureServices);
        }


        private static void ConfigureServices(IServiceCollection services)
        {
            var ipBlock = new IPBlacklistTrie();
            ipBlock.Add("127.0.0.1");
            ipBlock.Add("192.168.1.1/24");

            var appOptions = new ApplicationOptions();

            services.AddSingleton<ApplicationOptions>(appOptions);

            services.AddSingleton<IPBlacklistTrie>(ipBlock);

            services.AddTimeService();
            services.AddSingleton<TestService>();
            services.AddHostedService(provider => provider.GetRequiredService<TestService>());

            services.AddSingleton<ClientProcessService>();
            services.AddHostedService(provider => provider.GetRequiredService<ClientProcessService>());

            services.AddSingleton<GatewayProcessService>();
            services.AddHostedService(provider => provider.GetRequiredService<GatewayProcessService>());



            if (Environment.CommandLine.Contains("server"))
            {
                services.AddSingleton<ServerService>();
                services.AddHostedService(provider => provider.GetRequiredService<ServerService>());
            }

            if (Environment.CommandLine.Contains("client"))
            {
                services.AddSingleton<ClientService>();
                services.AddHostedService(provider => provider.GetRequiredService<ClientService>());
            }
        }

    }
}
