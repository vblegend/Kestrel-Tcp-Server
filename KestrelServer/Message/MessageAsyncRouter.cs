using KestrelServer.Network;
using Serilog;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;

namespace KestrelServer.Message
{

    public interface IMessageProcessor
    {

    }


    public delegate ValueTask MessageHandlerDelegate<TMesssage>(IConnectionSession session, TMesssage message) where TMesssage : AbstractNetMessage;


    /// <summary>
    /// 消息路由器
    /// </summary>
    public class MessageAsyncRouter
    {
        private const Int32 BaseIndex = 32767;
        private static ILogger logger = Log.ForContext<MessageAsyncRouter>();
        private readonly MessageHandlerDelegate<AbstractNetMessage>[] _messageHandlers = new MessageHandlerDelegate<AbstractNetMessage>[65535];
        private readonly IMessageProcessor _messageProcessor;

        /// <summary>
        /// 创建消息路由器并扫描processor内所有处理程序
        /// </summary>
        /// <param name="processor"></param>
        public MessageAsyncRouter(IMessageProcessor processor)
        {
            this._messageProcessor = processor;
            RegisterAllHandlers(processor);
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public void RegisterHandler<T>(MessageHandlerDelegate<T> action) where T : AbstractNetMessage, new()
        {
            RegisterHandler(MessagePool<T>.Kind, action.Method, (session, message) => action(session, (T)message));
        }


        /// <summary>
        /// 分发消息，路由至指定处理器
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public async ValueTask<Boolean> RouteAsync(IConnectionSession session, AbstractNetMessage message)
        {
            var handler = _messageHandlers[BaseIndex + message.Kind];
            if (handler == null) return false;
            await handler(session, message);
            return true;
        }


        private void RegisterHandler(Int16 kind, MethodInfo originMethod, MessageHandlerDelegate<AbstractNetMessage> messageHandler)
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
                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(IConnectionSession) && parameters[1].ParameterType.IsSubclassOf(typeof(AbstractNetMessage)))
                {
                    // 获取消息类型（第二个参数类型）
                    var messageType = parameters[1].ParameterType;
                    // 获取消息类型的 Kind
                    var kindField = typeof(MessagePool<>).MakeGenericType(messageType).GetField("Kind");
                    var kind = (Int16)kindField.GetValue(null);
                    // 动态创建 MessageHandlerDelegate<T> 委托
                    var handlerDelegate = CreateHandlerDelegate(messageType, method);
                    // 注册处理器
                    RegisterHandler(kind, method, handlerDelegate);
                }
            }
        }

        private MessageHandlerDelegate<AbstractNetMessage> CreateHandlerDelegate(Type messageType, MethodInfo method)
        {
            // 定义参数
            var sessionParam = Expression.Parameter(typeof(IConnectionSession), "session");
            var messageParam = Expression.Parameter(typeof(AbstractNetMessage), "message");
            // 将 AbstractNetMessage 转换为具体的消息类型
            var convertedMessageParam = Expression.Convert(messageParam, messageType);
            // 构建方法调用表达式
            var call = Expression.Call(Expression.Constant(this._messageProcessor), method, sessionParam, convertedMessageParam);
            // 如果方法返回值是 ValueTask，直接返回方法调用
            if (method.ReturnType == typeof(ValueTask))
            {
                // 创建 Lambda 表达式
                var lambda = Expression.Lambda<MessageHandlerDelegate<AbstractNetMessage>>(call, sessionParam, messageParam);
                return lambda.Compile();
            }
            else
            {
                // 如果方法返回值不是 ValueTask，抛出异常或转换为 ValueTask
                throw new InvalidOperationException("Method must return ValueTask.");
            }
        }

        // 创建对应类型的委托
        private MessageHandlerDelegate<AbstractNetMessage> CreateHandlerDelegate2(Type messageType, MethodInfo method)
        {
            // 定义参数
            var sessionParam = Expression.Parameter(typeof(IConnectionSession), "session");
            var messageParam = Expression.Parameter(typeof(AbstractNetMessage), "message");
            // 将 AbstractNetMessage 转换为具体的消息类型
            var convertedMessageParam = Expression.Convert(messageParam, messageType);
            // 构建方法调用表达式
            var call = Expression.Call(Expression.Constant(this._messageProcessor), method, sessionParam, convertedMessageParam);
            // 如果方法返回值是 ValueTask，则需等待
            if (method.ReturnType == typeof(ValueTask))
            {
                // 构建一个同步块，等待 ValueTask 完成
                var awaitExpression = Expression.Call(call, typeof(ValueTask).GetMethod("GetAwaiter"));
                var getResultCall = Expression.Call(awaitExpression, typeof(System.Runtime.CompilerServices.TaskAwaiter).GetMethod("GetResult"));
                // 将方法调用转换为无返回值的表达式块
                var block = Expression.Block(call, getResultCall);
                // 创建 Lambda 表达式
                var lambda = Expression.Lambda<MessageHandlerDelegate<AbstractNetMessage>>(block, sessionParam, messageParam);
                return lambda.Compile();
            }
            else
            {
                // 方法没有返回值的情况
                var lambda = Expression.Lambda<MessageHandlerDelegate<AbstractNetMessage>>(call, sessionParam, messageParam);
                return lambda.Compile();
            }
        }


    }


}
