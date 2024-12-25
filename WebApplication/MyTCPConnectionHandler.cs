using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Threading.Tasks;
using System;
using WebApplication;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Net;

namespace KestrelServer
{
    public class MyTCPConnectionHandler : TcpConnectionHandler
    {
        private readonly IPBlacklistTrie iPBlacklist;
        public MyTCPConnectionHandler(IPBlacklistTrie iPBlacklist)
        {
            this.iPBlacklist = iPBlacklist;
        }



        protected override async Task<Boolean> OnConnected(ConnectionContext connection)
        {
            if (connection.RemoteEndPoint is System.Net.IPEndPoint ipEndPoint)
            {
                var ipOfBytes = ipEndPoint.Address.GetAddressBytes();
                if (this.iPBlacklist.IsBlocked(ipEndPoint.Address))
                {
                    Console.WriteLine($"Blocked IP: {ipEndPoint.Address}");
                    return false;
                }
            }
            connection.Items.Add("Username", "root");
            Console.WriteLine($"Client connected: {connection.ConnectionId} {connection.RemoteEndPoint}");
            return true;
        }


        protected override async Task OnClose(ConnectionContext connection)
        {
            Console.WriteLine($"Client closed: {connection.ConnectionId}, ClientIp: {connection.RemoteEndPoint}, Username: {connection.Items["Username"]}");
        }


        protected override async Task OnError(ConnectionContext connection, Exception ex)
        {

        }

        protected override async Task OnReceive(ConnectionContext connection, ReadOnlySequence<Byte> buffer)
        {
            var result = GMessage.Parse(new SequenceReader<byte>(buffer), out GMessage message, out var packetLen);
            if (result == ParseResult.Illicit)
            {
                connection.Abort();
                return;
            }
            if (result == ParseResult.Ok)
            {
                if (++count % 1000 == 0)
                {
                    var text = $"Received packet: {count}";
                    Console.WriteLine(text);
                    var Output = connection.Transport.Output;
                    await Output.WriteAsync(Encoding.UTF8.GetBytes(text));
                    await Output.FlushAsync();

                }
            }
            message.Return();
        }
    }
}
