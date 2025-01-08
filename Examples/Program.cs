
using Examples;
using Examples.Services;
using Microsoft.Extensions.DependencyInjection;
using PacketNet.Message;
using Serilog;

namespace PacketNet
{



    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build().PacketLogger();
            await host.RunAsync();
            host.Dispose();
            Console.ReadLine();
        }


        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                 .UseSerilog(ConfigureSerilog)
                 .ConfigureServices(ConfigureServices);
        }

        private static void ConfigureSerilog(HostBuilderContext context, LoggerConfiguration configuration)
        {
            configuration.MinimumLevel.Is(Serilog.Events.LogEventLevel.Information);
            configuration.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning);
            configuration.WriteTo.Async(configure =>
            {
                configure.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss fff} [{Level:u4}] {Message:lj}{NewLine}{Exception}");//  [{SourceContext}]
            });
            configuration.Enrich.FromLogContext();

        }


        private static void ConfigureServices(IServiceCollection services)
        {
            var ipBlock = new IPBlacklistTrie();
            ipBlock.Add("127.0.0.1");
            ipBlock.Add("192.168.1.1/24");

            var appOptions = new ApplicationOptions("pipe");

            services.AddSingleton<ApplicationOptions>(appOptions);



            services.AddSingleton<IPBlacklistTrie>(ipBlock);
            services.AddSingleton<MessageResolver>(MessageResolver.Default);
            services.AddSingleton<GMessageParser>();

            services.AddTimeService();

            services.AddSingleton<MessageProcessor>();
            services.AddHostedService(provider => provider.GetRequiredService<MessageProcessor>());


            services.AddSingleton<TestMessageService>();
            services.AddHostedService(provider => provider.GetRequiredService<TestMessageService>());


            if (Environment.CommandLine.Contains("server"))
            {
                services.AddSingleton<TestServerService>();
                services.AddHostedService(provider => provider.GetRequiredService<TestServerService>());
            }

            if (Environment.CommandLine.Contains("client"))
            {
                services.AddSingleton<TestClientService>();
                services.AddHostedService(provider => provider.GetRequiredService<TestClientService>());
            }

            Console.WriteLine();

        }

    }
}
