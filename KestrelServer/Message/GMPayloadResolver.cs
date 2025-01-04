using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace KestrelServer.Message
{
    public class GMPayloadResolver
    {

        /// <summary>
        /// 默认的解析器
        /// </summary>
        public static GMPayloadResolver Default = new GMPayloadResolver();

        static GMPayloadResolver()
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
                else
                {
                    var genFunc = type.CreateDefaultConstructor<AbstractNetMessage>();
                    var obj = genFunc();
                    Keys.Add(obj.Kind, genFunc);
                    obj = null;
                }
            }
        }


        private readonly static Dictionary<Int32, Func<AbstractNetMessage>> Keys = new Dictionary<Int32, Func<AbstractNetMessage>>();




        public AbstractNetMessage Resolver(int action)
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
