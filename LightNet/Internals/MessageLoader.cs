using LightNet.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LightNet.Internals
{
    internal class MessageMakeInfo
    {
        public MessageMakeInfo(Type type, Int16 kind, IntPtr funcPointer)
        {
            this.Type = type;
            this.Kind = kind;
            this.FuncPointer = funcPointer;
        }
        public readonly Type Type;
        public readonly Int16 Kind;
        public readonly IntPtr FuncPointer;
    }



    internal static class MessageLoader
    {
        internal static List<MessageMakeInfo> InitializedTypes = new List<MessageMakeInfo>();

#pragma warning disable CA2255 // 不应在库中使用 “ModuleInitializer” 属性
        [ModuleInitializer]
#pragma warning restore CA2255 // 不应在库中使用 “ModuleInitializer” 属性
        public static void ScanMessageType()
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
            var processorTypes = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && type.IsPublic);
            foreach (var type in processorTypes)
            {
                var attribuute = type.GetCustomAttribute<MessageAttribute>();
                if (attribuute != null)
                {
                    // 初始化 消息池属性
                    Type genericType = typeof(MFactory<>).MakeGenericType(type);
                    var InitialFactory = genericType.GetMethod("InitialMessageType", BindingFlags.Static | BindingFlags.NonPublic);
                    var tryInit = (MFactoryInitialMethod)InitialFactory.CreateDelegate(typeof(MFactoryInitialMethod), null);
                    tryInit(attribuute);
                    var getmessageMethod = genericType.GetMethod("GetMessage", BindingFlags.Static | BindingFlags.Public);
                    IntPtr funcPointer = getmessageMethod.MethodHandle.GetFunctionPointer();
                    InitializedTypes.Add(new MessageMakeInfo(type, attribuute.Kind, funcPointer));
                }
            }
        }
    }
}
