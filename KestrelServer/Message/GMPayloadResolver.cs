using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.AccessControl;

namespace KestrelServer.Message
{
    public class GMPayloadResolver
    {
        class ProcessCache
        {
            public Func<INetMessage> Gen;
            public IGMessageProcessor Processor;
        }




        /// <summary>
        /// 默认的解析器
        /// </summary>
        public static GMPayloadResolver Default = new GMPayloadResolver();

        static GMPayloadResolver()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) ResolveAssembly(assembly);
            AppDomain.CurrentDomain.AssemblyLoad += AppDomain_AssemblyLoad;
        }

        private static void AppDomain_AssemblyLoad(object? sender, AssemblyLoadEventArgs args)
        {
            ResolveAssembly(args.LoadedAssembly);
        }

        private static void ResolveAssembly(Assembly assembly)
        {
            var processorTypes = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract);
            foreach (var type in processorTypes)
            {
                //var kindAttribute = type.GetCustomAttribute<MessageProcessor>();
                //if (kindAttribute == null) continue;
                //if (!type.GetInterfaces().Any(e => e == typeof(IGMessageProcessor))) continue;

                //var processor = (IGMessageProcessor)Activator.CreateInstance(type)!;
                //var genFunc = kindAttribute.PayloadType.CreateDefaultConstructor<IMessagePayload>();
                //Keys.Add(kindAttribute.Value, new ProcessCache() { Gen = genFunc, Processor = processor });
                //Console.WriteLine($"Register Processor: {kindAttribute.Kind}[{kindAttribute.Value}]  Payload Type: {kindAttribute.PayloadType.FullName}   Processor: {type.FullName}");
                if (!type.GetInterfaces().Any(e => e == typeof(INetMessage))) continue;
                var genFunc = type.CreateDefaultConstructor<INetMessage>();
                var obj = genFunc();
                Keys.Add((Int32)obj.Kind, genFunc);
                obj = null;
            }
        }


        private readonly static Dictionary<Int32, Func<INetMessage>> Keys = new Dictionary<Int32, Func<INetMessage>>();




        public INetMessage Resolver(int action)
        {
            if (Keys.TryGetValue(action, out var genFunc))
            {
              return genFunc();
            }


            //if (1920 == action) return new StringPayload();
            return new ExampleMessage(0);
        }



    }
}
