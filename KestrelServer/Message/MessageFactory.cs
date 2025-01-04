using System;


namespace KestrelServer.Message
{
    public static class MessageFactory
    {

        public static T Create<T>() where T : AbstractNetMessage, new()
        {
            return MessagePool<T>.Shared.Get();
        }


        public static ExampleMessage ExampleMessage(Int64 value)
        {
            var example = Create<ExampleMessage>();
            example.X = value;
            return example;
        }



    }
}
