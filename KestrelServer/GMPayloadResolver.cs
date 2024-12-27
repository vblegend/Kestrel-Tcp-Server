using System;

namespace KestrelServer
{
    public class GMPayloadResolver
    {


        public IMessagePayload Resolver(UInt32 action)
        {
            return new TestClass();
        }



    }
}
