﻿using Examples;
using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Light.Transmit.Adapters
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
        public static readonly UnPacketResult Invalid = new UnPacketResult(0, 0);



        /// <summary>
        /// 构造一个封包读取结果，给出已读数据长度和下次读取长度
        /// </summary>
        /// <param name="readLength"></param>
        /// <param name="nextReadLength"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public UnPacketResult(Int32 readLength, Int32 nextReadLength = 1)
        {
            if (nextReadLength < 1) throw new ArgumentOutOfRangeException(nameof(nextReadLength));
            ReadLength = readLength;
            NextReadLength = nextReadLength;
        }

        /// <summary>
        /// 成功读取的报文长度
        /// </summary>
        public readonly Int32 ReadLength;

        /// <summary>
        /// 下次需要的报文长度
        /// </summary>
        public readonly Int32 NextReadLength;

    }


    public delegate IConnectionSession AcceptCallback();


    /// <summary>
    /// 服务器事件适配器
    /// </summary>
    public abstract class ServerHandlerAdapter
    {

        public static readonly ServerHandlerAdapter None = new NoneServerHandlerAdapter();

        /// <summary>
        /// 新的客户端连接应答
        /// </summary>
        /// <param name="socket"></param>
        /// <returns> 返回false拒绝连接</returns>
        public abstract Boolean OnAccept(Socket socket);



        /// <summary>
        /// 新的客户端连接成功
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public abstract ValueTask OnConnected(IConnectionSession session);



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
        /// <param name="buffer"></param>
        /// <returns></returns>
        public abstract UnPacketResult OnPacket(IConnectionSession session, ReadOnlySequence<byte> buffer);


    }
}
