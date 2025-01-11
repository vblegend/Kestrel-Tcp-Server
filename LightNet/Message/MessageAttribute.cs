using System;

namespace LightNet.Message
{

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MessageAttribute : Attribute
    {
        public Int16 Kind { get; private set; }

        public Boolean UsePool { get; private set; }
        public Int32 PoolCapacity { get; private set; }


        /// <summary>
        /// 定义消息种类及消息池
        /// </summary>
        /// <param name="kind">定义消息的Kind</param>
        /// <param name="usePool">是否为消息启用消息池</param>
        /// <param name="poolCapacity">消息池最大容量，可通过 MFactory<GatewayPingMessage>.SetPoolMaxCapacity() 动态调整</param>
        public unsafe MessageAttribute(Int16 kind, Boolean usePool = false, Int32 poolCapacity = 64)
        {
            this.Kind = kind;
            this.UsePool = usePool;
            this.PoolCapacity = poolCapacity;
        }


    }



}
