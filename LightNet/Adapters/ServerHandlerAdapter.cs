using System;
using System.Buffers;
using System.Threading.Tasks;

namespace LightNet.Adapters
{
    /// <summary>
    /// 封包解析结果
    /// </summary>
    public enum ParseResult : int
    {
        /// <summary>
        /// 非法封包数据
        /// </summary>
        Illicit = 0,
        /// <summary>
        /// 部分封包，粘包、不完整的
        /// </summary>
        Partial = 1,
        /// <summary>
        /// 一个完整的封包
        /// </summary>
        Ok = 2,
    }



    /// <summary>
    /// 封包处理结果
    /// </summary>
    public readonly struct UnPacketResult
    {
        public UnPacketResult(bool complete, long length,long nextPacketSize = 0)
        {
            IsCompleted = complete;
            Length = length;
            NextPacketSize = nextPacketSize;
        }

        /// <summary>
        /// 待移除
        /// </summary>
        public readonly bool IsCompleted;

        /// <summary>
        /// 成功处理的报文长度
        /// </summary>
        public readonly long Length;

        /// <summary>
        /// 下次需要的报文长度
        /// </summary>
        public readonly long NextPacketSize;

    }


    /// <summary>
    /// 服务器事件适配器
    /// </summary>
    public abstract class ServerHandlerAdapter
    {


        /// <summary>
        /// 新的客户端连接事件
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public abstract ValueTask<bool> OnConnected(IConnectionSession session);



        /// <summary>
        /// 客户端连接关闭
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public abstract ValueTask OnClose(IConnectionSession session);



        /// <summary>
        /// Socket 不可恢复的异常
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public abstract ValueTask OnError(IConnectionSession session, Exception ex);



        /// <summary>
        /// 收到任意封包，进行自定义解析
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        //public abstract UnPacketResult OnPacket(IConnectionSession session, ref SequenceReader<byte> reader);



        public abstract UnPacketResult OnPacket(IConnectionSession session, ReadOnlySequence<byte> buffer);


    }
}
