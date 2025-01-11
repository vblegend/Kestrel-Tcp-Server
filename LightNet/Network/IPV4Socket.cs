using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace LightNet.Network
{
    public class IPV4Socket : IDisposable
    {
        protected readonly Socket socket;

        public static Int32 DEFAULT_SEND_BUFFER_SIZE = 1024 * 64;
        public static Int32 DEFAULT_RECEIVE_BUFFER_SIZE = 1024 * 64;

        protected IPV4Socket()
        {
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.SendBufferSize = DEFAULT_SEND_BUFFER_SIZE;
            this.ReceiveBufferSize = DEFAULT_RECEIVE_BUFFER_SIZE;
            this.NoDelay = true;
        }



        public virtual int ReceiveBufferSize
        {
            get { return (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer)!; }
            set { socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, value); }
        }

        // Gets or sets the size of the send buffer in bytes.
        public virtual int SendBufferSize
        {
            get { return (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer)!; }
            set { socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, value); }
        }

        // Gets or sets the receive time out value of the connection in milliseconds.
        public int ReceiveTimeout
        {
            get { return (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout)!; }
            set { socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, value); }
        }

        // Gets or sets the send time out value of the connection in milliseconds.
        public int SendTimeout
        {
            get { return (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout)!; }
            set { socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, value); }
        }

        // Gets or sets the value of the connection's linger option.
        [DisallowNull]
        public LingerOption LingerState
        {
            get { return socket.LingerState; }
            set { socket.LingerState = value!; }
        }

        // Enables or disables delay when send or receive buffers are full.
        public bool NoDelay
        {
            get { return socket.NoDelay; }
            set { socket.NoDelay = value; }
        }

        public virtual void Dispose()
        {
            if (socket != null)
            {
                socket.Dispose();
            }
        }
    }

}
