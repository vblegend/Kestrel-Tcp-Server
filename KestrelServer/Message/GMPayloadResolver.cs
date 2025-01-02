using System;

namespace KestrelServer.Message
{
    public class GMPayloadResolver
    {


        public IMessagePayload Resolver(uint action)
        {
            //if (1920 == action) return new StringPayload();
            return new ExamplePlayload();
        }



    }
}
