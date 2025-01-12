using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightNet.Message
{



    public delegate Boolean MessageMiddlewareNext(IConnectionSession session, AbstractNetMessage message);

    public abstract class MessageMiddleware
    {

        public abstract void OnMessage(IConnectionSession session, AbstractNetMessage message, MessageMiddlewareNext next);



        //public void ProcessMessage(IConnectionSession session, AbstractNetMessage message)
        //{
        //    // 创建一个中间件调用链
        //    MessageMiddlewareNext next = (s, m) => { return true; }; // 默认的next，不做任何处理
        //    for (int i = _middlewares.Count - 1; i >= 0; i--)
        //    {
        //        // 捕获当前的next，为当前的中间件创建一个新的next
        //        MessageMiddlewareNext currentNext = next;
        //        next = (s, m) =>
        //        {
        //            _middlewares[i].OnMessage(s, m, currentNext);
        //            return true; // 假设总是继续调用下一个（这取决于具体实现）
        //        };
        //    }

        //    // 调用第一个中间件
        //    next(session, message);
        //}
    }
}
