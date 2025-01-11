using System;

namespace LightNet.Message
{


    /// <summary>
    /// 消息池化选项
    /// </summary>
    public enum PoolingOptions
    {
        /// <summary>
        /// 默认 不使用池化
        /// </summary>
        Default = 1,

        /// <summary>
        /// 使用内置消息池
        /// </summary>
        Pooling = 2,

        /// <summary>
        /// 使用自定义池
        /// </summary>
        Costom = 3
    }


    /// <summary>
    /// 定义消息，绑定消息Kind
    /// </summary>

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MessageAttribute : Attribute
    {
        /// <summary>
        /// 消息Kind定义
        /// </summary>
        public readonly Int16 Kind;

        /// <summary>
        /// 消息池化选项
        /// </summary>
        public readonly PoolingOptions Pooling;

        /// <summary>
        /// 池最大容量  仅 Pooling = Pooling 时有效
        /// </summary>
        public readonly Int32 PoolCapacity;

        /// <summary>
        /// 自定义消息池类型
        /// </summary>
        public readonly Type CustomPoolType;


        /// <summary>
        /// 不使用消息池
        /// </summary>
        /// <param name="kind"></param>
        public MessageAttribute(Int16 kind)
        {
            this.Kind = kind;
            this.Pooling = PoolingOptions.Default;
            this.PoolCapacity = -1;
            this.CustomPoolType = null;
        }

        /// <summary>
        /// 支持默认消息池选项，使用默认消息池容量 Environment.ProcessorCount * 2
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="pooling"></param>
        public MessageAttribute(Int16 kind, PoolingOptions pooling)
        {
            this.Kind = kind;
            this.Pooling = pooling;
            this.CustomPoolType = null;
            this.PoolCapacity = Environment.ProcessorCount * 2;
        }

        /// <summary>
        /// 支持自定义消息池
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="pooling"></param>
        /// <param name="poolType"></param>
        public MessageAttribute(Int16 kind, PoolingOptions pooling, Type poolType)
        {
            this.Kind = kind;
            this.Pooling = pooling;
            this.PoolCapacity = -1;
            this.CustomPoolType = poolType;
        }

        /// <summary>
        /// 支持默认消息池
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="pooling"></param>
        /// <param name="poolCapacity">消息池容量 pooling 为 Pooling时有效  可通过 MFactory<GatewayPingMessage>.SetPoolMaxCapacity() 动态调整</param>
        public MessageAttribute(Int16 kind, PoolingOptions pooling, Int32 poolCapacity)
        {
            this.Kind = kind;
            this.Pooling = pooling;
            this.PoolCapacity = poolCapacity;
            this.CustomPoolType = null;
        }


    }



}
