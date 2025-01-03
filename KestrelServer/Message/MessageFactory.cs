using System;
using System.Diagnostics.Metrics;

namespace KestrelServer.Message
{
    public static class MessageFactory
    {

        public static T Create<T>() where T : AbstractNetMessage, new()
        {
            return (T)MessagePool<T>.Proxy.Get();
        }


        public static ExampleMessage ExampleMessage(Int64 value)
        {
            var example = Create<ExampleMessage>();
            example.X = value;
            return example;
        }



    }
}
