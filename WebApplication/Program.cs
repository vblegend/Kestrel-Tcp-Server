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
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace WebApplication
{
    public class Program
    {

        public static (byte FirstByte, uint RemainingBytes) SplitUint32(uint value)
        {
            // ��ȡ��һ�ֽ�
            byte firstByte = (byte)((value >> 24) & 0xFF);
            // ��ȡʣ�������ֽ�
            uint remainingBytes = value & 0x00FFFFFF;
            return (firstByte, remainingBytes ^ 0xFFFFFF);
        }

        public static uint Combine(byte firstByte, uint remainingBytes)
        {
            // ����ʣ���ֽ�ֻռ�� 3 ���ֽ�
            remainingBytes &= 0x00FFFFFF;
            // ����һ���ֽ������� 8 λ������ʣ���ֽںϲ�
            return ((uint)firstByte << 24) | (remainingBytes ^ 0xFFFFFF);
        }

  
        public static void Main(string[] args)
        {
            GMessage gMessage = GMessage.Create(12345678, [1, 2, 3, 4, 5], [1, 2, 3, 4, 5, 6, 7, 8, 9, 255]);
            gMessage.SerialNumber = 1000000234;
        
            using (var stream = StreamPool.GetStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    gMessage.Write(writer);
                    gMessage.Return();
                }
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
            services.AddSingleton<IPBlacklistTrie>(ipBlock);

        }

        private static void ConfigureWebHostDefaults(IWebHostBuilder webBuilder)
        {
            webBuilder.ConfigureKestrel(ConfigureKestrel);
            webBuilder.Configure(ConfigureApplication);
        }


        private static void ConfigureKestrel(KestrelServerOptions options)
        {
            // ʹ�� Sockets ���䲢����ѡ��
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
