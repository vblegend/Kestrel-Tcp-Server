using KestrelServer.Network;

namespace KestrelServer.Message
{

    public abstract class AbstractMessageProcessor
    {
        public void Process(IConnectionSession session, AbstractNetMessage message)
        {
            if (message.Kind == MessagePool<GatewayMessage>.Kind) GatewayProcess(session, (GatewayMessage)message);
            if (message.Kind == MessagePool<ExampleMessage>.Kind) ExampleProcess(session, (ExampleMessage)message);
        }
        protected abstract void ExampleProcess(IConnectionSession session, ExampleMessage message);
        protected abstract void GatewayProcess(IConnectionSession session, GatewayMessage message);

    }




    public class DefaultMessageProcessor : AbstractMessageProcessor
    {
        protected override void ExampleProcess(IConnectionSession session, ExampleMessage message)
        {

        }

        protected override void GatewayProcess(IConnectionSession session, GatewayMessage message)
        {

        }
    }
}
