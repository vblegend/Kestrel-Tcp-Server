using KestrelServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace WebApplication
{


    public class TestClass : ISerializer
    {
        public Int32 X = 123456789;
        public Int32 Y = 987654321;

        public void Read(BinaryReader reader)
        {
            X = reader.ReadInt32();
            Y = reader.ReadInt32();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
        }
    }



    public class Program
    {
        // 
        public static (byte FirstByte, uint RemainingBytes) SplitUint32(uint value)
        {



            // 获取第一字节
            byte firstByte = (byte)((value >> 24) & 0xFF);
            // 获取剩余三个字节
            uint remainingBytes = value & 0x00FFFFFF;
            return (firstByte, remainingBytes ^ 0xFFFFFF);
        }

        public static uint Combine(byte firstByte, uint remainingBytes)
        {
            // 限制剩余字节只占低 3 个字节
            remainingBytes &= 0x00FFFFFF;
            // 将第一个字节移至高 8 位，并与剩余字节合并
            return ((uint)firstByte << 24) | (remainingBytes ^ 0xFFFFFF);
        }

        public static void Main(string[] args)
        {
            GMessage gMessage = GMessage.Create(12345678, new TestClass());

            using (var stream = StreamPool.GetStream())
            {

                gMessage.WriteToAsync(stream).Wait();
                gMessage.Return();

                var span = stream.GetBuffer();
                for (int i = 0; i < stream.Length; i++)
                {
                    Console.Write(span[i].ToString("X2"));
                }
                Console.WriteLine();
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(stream.ToArray()));
                GMessage.Parse(reader, out var msg, out var value);
                msg.Return();
            }

            CreateHostBuilder(args).Build().Run();
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



            services.AddSingleton<TimeService>();
            services.AddHostedService(provider => provider.GetRequiredService<TimeService>());


            services.AddSingleton<IPBlacklistTrie>(ipBlock);

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
