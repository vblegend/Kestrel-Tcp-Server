using Examples.Client;
using Examples.Gateway;
using LightNet.Message;


namespace Examples
{
    public static class MessageFactory
    {
        public static T Create<T>() where T : AbstractNetMessage, new()
        {
            return MFactory<T>.GetMessage();
        }


        public static ClientMessage ExampleMessage(Int64 value)
        {
            var msg = MFactory<ClientMessage>.GetMessage();
            msg.X = value;
            return msg;
        }


        public static GatewayAuthRequestMessage GatewayAuthRequestMessage(String pwd)
        {
            var msg = MFactory<GatewayAuthRequestMessage>.GetMessage();
            msg.Pwd = pwd;
            return msg;
        }


    }
}
