
using Examples;
using Examples.Services;
using LightNet.Message;
using Serilog;

namespace LightNet
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

            var appOptions = new ApplicationOptions();

            services.AddSingleton<ApplicationOptions>(appOptions);

            services.AddSingleton<IPBlacklistTrie>(ipBlock);
            services.AddSingleton<MessageResolver>(MessageResolver.Default);
            services.AddSingleton<MessageParser>();

            services.AddTimeService();



            services.AddSingleton<TestService>();
            services.AddHostedService(provider => provider.GetRequiredService<TestService>());


            services.AddSingleton<ProcessService>();
            services.AddHostedService(provider => provider.GetRequiredService<ProcessService>());



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

            Console.WriteLine();

        }

    }
}
