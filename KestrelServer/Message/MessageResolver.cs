using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace KestrelServer.Message
{
    public class MessageResolver
    {

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

        private static void ResolveAssembly(Assembly assembly)
        {
            var processorTypes = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract);
            foreach (var type in processorTypes)
            {
                if (!type.IsSubclassOf(typeof(AbstractNetMessage))) continue;
                var attrubute = type.GetCustomAttribute<IPoolGetterAttribute>();
                if (attrubute != null)
                {
                    var msg = attrubute.Getter();
                    Keys.Add(msg.Kind, attrubute.Getter);
                    msg.Return();
                }
            }
        }


        private readonly static Dictionary<Int16, Func<AbstractNetMessage>> Keys = new Dictionary<Int16, Func<AbstractNetMessage>>();




        public AbstractNetMessage Resolver(Int16 action)
        {
            if (Keys.TryGetValue(action, out var genFunc))
            {
                return genFunc();
            }
            //
            //
            //
            throw new InvalidOperationException();
        }



    }
}
