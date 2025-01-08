
using Examples;
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

            services.AddSingleton<IPBlacklistTrie>(ipBlock);
            services.AddSingleton<MessageResolver>(MessageResolver.Default);
            services.AddSingleton<GMessageParser>();





            services.AddTimeService();

            services.AddSingleton<MessageProcessor>();
            services.AddHostedService(provider => provider.GetRequiredService<MessageProcessor>());

            if (Environment.CommandLine.Contains("server"))
            {
                services.AddSingleton<ExampleServer>();
                services.AddHostedService(provider => provider.GetRequiredService<ExampleServer>());
            }


            if (Environment.CommandLine.Contains("client"))
            {
                services.AddSingleton<TestService>();
                services.AddHostedService(provider => provider.GetRequiredService<TestService>());
            }
            Console.WriteLine();

        }

    }
}
