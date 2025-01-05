using KestrelServer.Network;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace KestrelServer.Message
{

    public delegate void MessageHandlerDelegate<TMesssage>(IConnectionSession session, TMesssage message) where TMesssage : AbstractNetMessage;


    public abstract class AbstractMessageProcessor
    {
        private readonly Dictionary<int, MessageHandlerDelegate<AbstractNetMessage>> _messageHandlers = new Dictionary<int, MessageHandlerDelegate<AbstractNetMessage>>();

        public void RegisterHandler<T>(MessageHandlerDelegate<T> action) where T : AbstractNetMessage, new()
        {
            _messageHandlers[MessagePool<T>.Kind] = (session, message) => action(session, (T)message);
        }

        public void Process(IConnectionSession session, AbstractNetMessage message)
        {
            if (_messageHandlers.TryGetValue(message.Kind, out var handler))
            {
                handler(session, message);
            }
            else
            {
                throw new InvalidOperationException("No handler found for message kind " + message.Kind);
            }
        }



        // 自动注册类中的所有处理方法
        public void RegisterAllHandlers()
        {
            // 获取当前类的所有方法
            var methods = this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
                    var kind = (int)kindField.GetValue(null);
                    // 动态创建 MessageHandlerDelegate<T> 委托
                    var handlerDelegate = CreateHandlerDelegate(messageType, method);
                    // 注册处理器
                    _messageHandlers[kind] = handlerDelegate;
                }
            }
        }


        // 创建对应类型的委托
        private MessageHandlerDelegate<AbstractNetMessage> CreateHandlerDelegate(Type messageType, MethodInfo method)
        {
            // 定义参数
            var sessionParam = Expression.Parameter(typeof(IConnectionSession), "session");
            var messageParam = Expression.Parameter(typeof(AbstractNetMessage), "message");
            // 强制转换 messageParam 为 AbstractNetMessage 类型 messageType
            var convertedMessageParam = Expression.Convert(messageParam, messageType);
            // 创建方法调用的表达式
            var call = Expression.Call(Expression.Constant(this), method, sessionParam, convertedMessageParam);
            // 创建 Lambda 表达式
            var lambda = Expression.Lambda<MessageHandlerDelegate<AbstractNetMessage>>(call, sessionParam, messageParam);
            // 编译并返回委托
            return lambda.Compile();
        }

    }



    public class DefaultMessageProcessor : AbstractMessageProcessor
    {

        public DefaultMessageProcessor()
        {
            //RegisterHandler<GatewayMessage>(GatewayProcess);
            //RegisterHandler<ExampleMessage>(ExampleProcess);
            RegisterAllHandlers();
        }


        public void ExampleProcess(IConnectionSession session, ExampleMessage message)
        {

        }

        public void GatewayProcess(IConnectionSession session, GatewayMessage message)
        {

        }
    }
}
