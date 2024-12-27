using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.Threading.Tasks;
using System;

using System.Text;


namespace KestrelServer
{
    public class MyTCPConnectionHandler : TcpConnectionHandler
    {
        private readonly IPBlacklistTrie iPBlacklist;
        private readonly TimeService timeService;
        private readonly GMessageParser messageParser;

        public MyTCPConnectionHandler(IPBlacklistTrie iPBlacklist, TimeService timeService, GMessageParser messageParser)
        {
            this.timeService = timeService;
            this.iPBlacklist = iPBlacklist;
            this.messageParser = messageParser;
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
            var time = this.timeService.Now();
            Console.WriteLine($"{time} Client connected: {connection.ConnectionId} {connection.RemoteEndPoint}");
            await connection.Send(GMessage.Create(1001, [111, 222, 333, 444], Encoding.UTF8.GetBytes(time.ToString("yyyy-MM-dd HH:mm:ss"))));
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
            var result = messageParser.Parse(new SequenceReader<byte>(buffer), out GMessage message);
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
