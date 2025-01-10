using LightNet.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LightNet.Internals
{
    internal class MessageMakeInfo
    {
        public Type Type;
        public Int16 Kind;
        public IntPtr FuncPointer;
    }



    internal static class MessageLoader
    {
        internal static List<MessageMakeInfo> InitializedTypes = new List<MessageMakeInfo>();

        static MessageLoader()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) ResolveAssembly(assembly);
            AppDomain.CurrentDomain.AssemblyLoad += AppDomain_AssemblyLoad;
        }
        private static void AppDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            ResolveAssembly(args.LoadedAssembly);
        }

        private static unsafe void ResolveAssembly(Assembly assembly)
        {
            var processorTypes = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract);
            foreach (var type in processorTypes)
            {
                var attribuute = type.GetCustomAttribute<MessageAttribute>();
                if (attribuute != null)
                {
                    // 初始化 消息池属性
                    Type genericType = typeof(MFactory<>).MakeGenericType(type);
                    var InitialFactory = genericType.GetMethod("InitialFactory", BindingFlags.Static | BindingFlags.NonPublic);
                    var tryInit = (MFactoryInitialMethod)InitialFactory.CreateDelegate(typeof(MFactoryInitialMethod), null);
                    tryInit(attribuute.Kind, attribuute.PoolCapacity);
                    var getmessageMethod = genericType.GetMethod("GetMessage", BindingFlags.Static | BindingFlags.Public);
                    IntPtr funcPointer = getmessageMethod.MethodHandle.GetFunctionPointer();
                    InitializedTypes.Add(new MessageMakeInfo()
                    {
                        Type = type,
                        Kind = attribuute.Kind,
                        FuncPointer = funcPointer
                    });
                }
            }
        }
    }
}
