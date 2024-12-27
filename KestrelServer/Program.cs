using KestrelServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KestrelServer
{


    public struct TestClass : IMessagePayload
    {
        public Int32 X = 123;
        public String Text = "Hello";
        public Int32 Y = 321;

        public TestClass()
        {
        }

        public void Read(SequenceReader<byte> reader)
        {
            reader.TryRead<Int32>(out X);
            reader.TryReadString(out Text);
            reader.TryRead<Int32>(out Y);
        }


        public void Write(IBufferWriter<byte> writer)
        {
            writer.Write(X);
            writer.Write(Text, Encoding.UTF8);
            writer.Write(Y);
        }
    }



    public class Program
    {
        private static CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Shutdown Requested...");
                _cancellationSource.Cancel(true);
                eventArgs.Cancel = true;
            };

            try
            {
                await host.RunAsync(_cancellationSource.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Application is shutting down...");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                await host.StopAsync();
                host.Dispose();
            }
            Console.ReadLine();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                 .ConfigureLogging(ConfigureLogging)
                 .ConfigureServices(ConfigureServices)
                 .ConfigureWebHostDefaults(ConfigureWebHostDefaults);
        }


        private static void ConfigureLogging(ILoggingBuilder logging)
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Error);
            logging.AddFilter("Microsoft.AspNetCore.Mvc.Infrastructure", LogLevel.None);
            logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Error);
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



            services.AddSingleton<TimeService>();
            services.AddHostedService(provider => provider.GetRequiredService<TimeService>());





            //services.AddSingleton<DynamicKestrelListenerService>();
            //services.AddHostedService(provider => provider.GetRequiredService<DynamicKestrelListenerService>());


        }

        private static void ConfigureWebHostDefaults(IWebHostBuilder webBuilder)
        {
            webBuilder.ConfigureKestrel(ConfigureKestrel);
            webBuilder.Configure(ConfigureApplication);
        }


        private static void ConfigureKestrel(KestrelServerOptions options)
        {
            // 使用 Sockets 传输并配置选项
            options.Limits.MaxRequestBufferSize = 64 * 1024;
            options.Limits.MaxResponseBufferSize = 64 * 1024;
            options.Limits.MaxConcurrentConnections = 10;
            options.Listen(IPAddress.Any, 50000, listenOptions =>
            {
                 listenOptions.UseConnectionHandler<MyTCPConnectionHandler>();
            });

        }



        private static void ConfigureApplication(IApplicationBuilder applicationBuilder)
        {
            //app.Use(async (HttpContext context, RequestDelegate next) =>
            //{
            //    await context.Response.WriteAsync("TCP Server is running.");
            //});
        }




    }
}
