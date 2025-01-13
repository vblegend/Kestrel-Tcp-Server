using Examples.Gateway;
using System;
using System.Buffers;


namespace LightNet.Message.Secure
{
    internal class GatewayAuthMiddleware : MessageMiddleware
    {
        private readonly String _authorization;
        public GatewayAuthMiddleware(String authorization)
        {
            _authorization = authorization;
        }

        public override Boolean OnMessage(IConnectionSession session, AbstractNetMessage message)
        {
            if (session.Datas[4] != null)
            {
                if (message.Kind == GatewayMessageKind.AuthRequest)
                {
                    session.Close(SessionShutdownCause.ILLEGAL_OPERATOR);
                    return false;
                }
                return true;
            }
            if (message.Kind != GatewayMessageKind.AuthRequest)
            {
                session.Close(SessionShutdownCause.ILLEGAL_DATA);
                return false;
            }
            var msg = (GatewayAuthRequestMessage)message;
            var isSuccess = msg.Pwd == _authorization;
            if (isSuccess)
            {
                session.Datas[4] = true;
                var response = MFactory<GatewayAuthResponseMessage>.CreateRaw();
                response.Code = 9981;
                session.WriteFlushAsync(response).GetAwaiter().GetResult();
            }
            else
            {
                session.Close(SessionShutdownCause.ERROR);
            }
            return false;
        }
    }
}
