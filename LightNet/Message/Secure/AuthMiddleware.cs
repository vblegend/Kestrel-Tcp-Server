using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightNet.Message.Secure
{
    internal class AuthMiddleware : MessageMiddleware
    {
        public unsafe override void OnMessage(IConnectionSession session, AbstractNetMessage message, MessageMiddlewareNext next)
        {
            if (session.Datas[4] != null)
            {
                next(session, message);
                return;
            }
      

        }
    }
}
