using System;

namespace KestrelServer.Message
{
    public class GMPayloadResolver
    {


        public IMessagePayload Resolver(uint action)
        {
            return new TestClass();
        }



    }
}
