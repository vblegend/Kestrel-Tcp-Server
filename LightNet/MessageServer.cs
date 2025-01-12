﻿using LightNet.Adapters;
using LightNet.Message;
using LightNet.Network;
using LightNet.Pipes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Threading.Tasks;



namespace LightNet
{

    public abstract class MessageServer : ServerHandlerAdapter
    {
        private const Int32 MINIMUM_PACKET_LENGTH = 5;
        private readonly ILogger<TCPServer> logger = LoggerProvider.CreateLogger<TCPServer>();
        private IPacketServer _packetServer;
        public readonly MessageResolver messageResolver;
        private string authorization;

        protected MessageServer(MessageResolver resolver)
        {
            messageResolver = resolver;
        }



        /// <summary>
        /// 支持 tcp 和 pipe
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Listen(Uri uri)
        {
            if (uri == null) throw new ArgumentNullException("uri");
            switch (uri.Scheme.ToLower())
            {
                case "tcp":
                    {
                        _packetServer = new TCPServer();
                        break;
                    }
                case "pipe":
                    {
                        _packetServer = new PipeServer();
                        break;
                    }
                default:
                    throw new ArgumentNullException("uri");
            }
            var querys = QueryHelpers.ParseQuery(uri.Query);
            if (querys.TryGetValue("pwd", out var pwd))
            {
                this.authorization = pwd;
            }
            _packetServer.SetAdapter(this);
            _packetServer.Listen(uri);
        }

        public async Task StopAsync()
        {
            if (_packetServer != null) await _packetServer.StopAsync();
            _packetServer = null;
        }


        public override UnPacketResult OnPacket(IConnectionSession session, ReadOnlySequence<byte> buffer)
        {
            Int32 len = 0;
            var bufferReader = new SequenceReader<byte>(buffer);
            while (bufferReader.Remaining >= MINIMUM_PACKET_LENGTH)
            {
                var result = messageResolver.TryReadMessage(ref bufferReader, out AbstractNetMessage message, out var length);
                if (result == ParseResult.Ok)
                {
                    message.Session = session;
                    // 
                    OnReceive(session, message);
                }
                else if (result == ParseResult.Partial)
                {
                    return new UnPacketResult(len, length);
                }
                else if (result == ParseResult.Illicit)
                {
                    throw new IllegalDataException("Illegal packet detected. Connection to be closed.");
                }
                len += length;
            }
            return new UnPacketResult(len, MINIMUM_PACKET_LENGTH);
        }

        public abstract void OnReceive(IConnectionSession session, AbstractNetMessage message);

    }
}
