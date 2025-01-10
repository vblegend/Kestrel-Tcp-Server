using System;

namespace LightNet.Message
{

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MessageAttribute : Attribute
    {
        public Int16 Kind { get; private set; }
        public Int32 PoolCapacity { get; private set; }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="kind">定义消息的Kind</param>
        /// <param name="poolCapacity">池容量，如果为0则禁用池</param>
        public unsafe MessageAttribute(Int16 kind, Int32 poolCapacity = 0)
        {
            this.Kind = kind;
            this.PoolCapacity = poolCapacity;
        }


    }



}
