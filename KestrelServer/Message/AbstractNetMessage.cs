using KestrelServer.Network;
using System;
using System.Buffers;


namespace KestrelServer.Message
{
    /// <summary>
    /// 网络消息基类
    /// </summary>
    public abstract class AbstractNetMessage
    {
        /// <summary>
        /// 消息头的定义魔法数
        /// </summary>
        public static readonly UInt16 Header = 0x4D47;

        /// <summary>
        /// 获取消息的Kind
        /// </summary>
        public readonly Int16 Kind;

        /// <summary>
        /// 池子的归还函数
        /// </summary>
        internal IMessagePool _pool;

        /// <summary>
        /// 
        /// </summary>
        public IConnectionSession Session;

        /// <summary>
        /// 从缓存读取消息内容
        /// </summary>
        /// <param name="reader"></param>
        public abstract void Read(SequenceReader<byte> reader);

        /// <summary>
        /// 将消息内容写入缓存
        /// </summary>
        /// <param name="writer"></param>
        public abstract void Write(IBufferWriter<byte> writer);

        /// <summary>
        /// 归还消息（如果消息是在池中的话）并清理消息内容。
        /// </summary>
        public void Return()
        {
            Reset();
            Session = null;
            if (_pool != null) _pool.Return(this);
        }

        /// <summary>
        /// 重置消息内容等待被复用
        /// </summary>
        public virtual void Reset()
        {

        }

    }

}
