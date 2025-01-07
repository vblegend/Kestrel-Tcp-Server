using Serilog;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;

namespace PacketNet.Message
{

    public interface IMessageProcessor
    {

    }


    public delegate ValueTask AsyncMessageHandlerDelegate<TMesssage>(TMesssage message) where TMesssage : AbstractNetMessage;


    /// <summary>
    /// 消息路由器
    /// </summary>
    public class AsyncMessageRouter
    {
        private const Int32 BaseIndex = 32767;
        private static ILogger logger = Log.ForContext<AsyncMessageRouter>();
        private readonly AsyncMessageHandlerDelegate<AbstractNetMessage>[] _messageHandlers = new AsyncMessageHandlerDelegate<AbstractNetMessage>[65535];
        private readonly IMessageProcessor _messageProcessor;

        /// <summary>
        /// 创建消息路由器并扫描processor内所有处理程序
        /// </summary>
        /// <param name="processor"></param>
        public AsyncMessageRouter(IMessageProcessor processor)
        {
            this._messageProcessor = processor;
            RegisterAllHandlers(processor);
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public void RegisterHandler<T>(AsyncMessageHandlerDelegate<T> action) where T : AbstractNetMessage, new()
        {
            RegisterHandler(MFactory<T>.Kind, action.Method, (message) => action((T)message));
        }


        /// <summary>
        /// 分发消息，路由至指定处理器
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public async ValueTask<Boolean> RouteAsync(AbstractNetMessage message)
        {
            try
            {
                var handler = _messageHandlers[BaseIndex + message.Kind];
                if (handler == null) return false;
                await handler(message);
            }
            finally
            {
                message.Return();
            }
            return true;
        }


        private void RegisterHandler(Int16 kind, MethodInfo originMethod, AsyncMessageHandlerDelegate<AbstractNetMessage> messageHandler)
        {
            if (_messageHandlers[BaseIndex + kind] != null)
            {
                logger.Warning("a message handler kind:[{0}] is overwritten..", kind);
            }
            logger.Information("Register Message Handler [{0}] {1}.{2}", kind, originMethod.DeclaringType.Name, originMethod.Name);
            _messageHandlers[BaseIndex + kind] = messageHandler;
        }

        /// <summary>
        /// 自动注册类中的所有处理方法
        /// </summary>
        private void RegisterAllHandlers(IMessageProcessor processor)
        {
            // 获取当前类的所有方法
            var methods = processor.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in methods)
            {
                // 方法必须符合以下条件：
                // - 参数列表包含 IConnectionSession 和消息类型（AbstractNetMessage）
                // - 返回值是 void
                var parameters = method.GetParameters();
                if (method.ReturnType == typeof(ValueTask) && parameters.Length == 1 && parameters[0].ParameterType.IsSubclassOf(typeof(AbstractNetMessage)))
                {
                    // 获取消息类型（第二个参数类型）
                    var messageType = parameters[0].ParameterType;
                    // 获取消息类型的 Kind
                    var kindField = typeof(MFactory<>).MakeGenericType(messageType).GetField("Kind");
                    var kind = (Int16)kindField.GetValue(null);
                    // 动态创建 MessageHandlerDelegate<T> 委托
                    var handlerDelegate = CreateHandlerDelegate(messageType, method);
                    // 注册处理器
                    RegisterHandler(kind, method, handlerDelegate);
                }
            }
        }

        private AsyncMessageHandlerDelegate<AbstractNetMessage> CreateHandlerDelegate(Type messageType, MethodInfo method)
        {
            // 定义参数
            var messageParam = Expression.Parameter(typeof(AbstractNetMessage), "message");
            // 将 AbstractNetMessage 转换为具体的消息类型
            var convertedMessageParam = Expression.Convert(messageParam, messageType);
            // 构建方法调用表达式
            var call = Expression.Call(Expression.Constant(this._messageProcessor), method, convertedMessageParam);
            // 如果方法返回值是 ValueTask，直接返回方法调用
            if (method.ReturnType == typeof(ValueTask))
            {
                // 创建 Lambda 表达式
                var lambda = Expression.Lambda<AsyncMessageHandlerDelegate<AbstractNetMessage>>(call, messageParam);
                return lambda.Compile();
            }
            else
            {
                // 如果方法返回值不是 ValueTask，抛出异常或转换为 ValueTask
                throw new InvalidOperationException("Method must return ValueTask.");
            }
        }

    }


}
