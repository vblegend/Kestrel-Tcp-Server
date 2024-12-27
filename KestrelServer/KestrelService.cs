using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting.Server;

namespace KestrelServer
{
    public class DynamicKestrelListenerService : IHostedService
    {
        private IWebHost? _webHost;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptions<ServiceCollection> _kestrelOptions;
        private readonly IServer _server;

        public DynamicKestrelListenerService(
            IServer server,
            IOptions<ServiceCollection> kestrelOptions,
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILoggerFactory loggerFactory)
        {
            _kestrelOptions = kestrelOptions;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _loggerFactory = loggerFactory;
            _server = server;
        }



        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _webHost = new WebHostBuilder()

                .UseConfiguration(_configuration)
                .ConfigureServices(services =>
                {
                    Console.WriteLine(services.GetType().FullName);
                    Console.WriteLine(_serviceProvider.GetType().FullName);
                    services.AddSingleton(_serviceProvider);
                    foreach (var descriptor in _kestrelOptions.Value.ToArray())
                    {
                        services.Add(descriptor);
                    }
                })
                .Configure(app => { })
                .UseKestrel((KestrelServerOptions options) =>
                {
                    options.Listen(IPAddress.Any, 50000, listenOptions =>
                    {
                        listenOptions.UseConnectionHandler<MyTCPConnectionHandler>();
                    });
                })
                .Build();

            Console.WriteLine("Kestrel Listener started.");
            await _webHost.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Kestrel Listener stopped.");
            if (_webHost != null)
            {
                await _webHost.StopAsync(cancellationToken);
                _webHost.Dispose();
                _webHost = null;
            }

            await Task.CompletedTask;
        }
    }

}
