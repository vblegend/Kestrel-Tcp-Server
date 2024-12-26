using KestrelServer;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Threading.Tasks;

namespace WebApplication
{



    public abstract class TcpConnectionHandler : ConnectionHandler
    {


        public Int64 count = 0;

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            var allowConnect = await this.OnConnected(connection);
            if (!allowConnect)
            {
                connection.Abort();
                return;
            }
            try
            {
                long minimumReadSize = GMessage.MinimumSize;
                while (true)
                {
                    var Input = connection.Transport.Input;
                    var Output = connection.Transport.Output;
                    var result = await Input.ReadAtLeastAsync((int)minimumReadSize);
                    if (result.IsCompleted) break;
                    var len = GMessage.ReadLength(new SequenceReader<byte>(result.Buffer));
                    if (len == UInt32.MaxValue || len > 64 * 1024)
                    {
                        await OnError(connection, new Exception("检测到非法封包，即将关闭连接！"));
                        return;
                    }
                    if (result.Buffer.Length < len)
                    {
                        minimumReadSize = len;
                        Input.AdvanceTo(result.Buffer.Start);
                        continue;
                    }
                    var packetData = result.Buffer.Slice(0, len);
                    await OnReceive(connection, packetData);
                    Input.AdvanceTo(result.Buffer.GetPosition(len));
                    minimumReadSize = GMessage.MinimumSize;
                }
            }
            catch (ConnectionResetException _)
            {

            }
            catch (Exception ex)
            {
                await this.OnError(connection, ex);
            }
            finally
            {
                await this.OnClose(connection);
            }

        }


        protected virtual Task<Boolean> OnConnected(ConnectionContext connection)
        {
            return Task.FromResult(true);
        }


        protected virtual async Task OnClose(ConnectionContext connection)
        {
            await Task.CompletedTask;
        }

        protected virtual async Task OnError(ConnectionContext connection, Exception ex)
        {
            await Task.CompletedTask;
        }

        protected virtual async Task OnReceive(ConnectionContext connection, ReadOnlySequence<Byte> buffer)
        {
            await Task.CompletedTask;
        }


    }


}
