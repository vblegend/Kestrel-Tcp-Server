using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace PacketNet.Message
{
    public class MessageResolver
    {
        private readonly static Dictionary<Int16, IntPtr> Keys = new();

        /// <summary>
        /// 默认的解析器
        /// </summary>
        public static MessageResolver Default = new MessageResolver();

        static MessageResolver()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) ResolveAssembly(assembly);
            AppDomain.CurrentDomain.AssemblyLoad += AppDomain_AssemblyLoad;
        }

        private static void AppDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            ResolveAssembly(args.LoadedAssembly);
        }

        private unsafe static void ResolveAssembly(Assembly assembly)
        {
            var processorTypes = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract);
            foreach (var type in processorTypes)
            {
                if (!type.IsSubclassOf(typeof(AbstractNetMessage))) continue;

                // 获取所有的 Attribute
                var attributes = type.GetCustomAttributes(inherit: true);
                // 查找实现 IMessageAttribute 的属性
                var messageAttribute = attributes.OfType<IMessageAttribute>().FirstOrDefault();
                if (messageAttribute != null)
                {
                    var getter = messageAttribute.GetPointer();
                    var msg = getter();
                    Keys.Add(msg.Kind, (IntPtr)getter);
                    msg.Return();
                }
            }
        }




        public unsafe AbstractNetMessage Resolver(Int16 action)
        {
            if (Keys.TryGetValue(action, out var p))
            {
                delegate*<AbstractNetMessage> getter = (delegate*<AbstractNetMessage>)p;
                return getter();
            }
            throw new InvalidOperationException();
        }



    }
}
