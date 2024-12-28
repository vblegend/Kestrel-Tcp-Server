using Microsoft.Extensions.Hosting;
using System;
using System.Threading;

namespace Microsoft.Extensions.Hosting
{
    public static class HostExtensions
    {
        public static void UseCtrlCCancel(this IHost host,  CancellationTokenSource cancellationSource)
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                cancellationSource.Cancel(true);
                eventArgs.Cancel = true;
            };
        }





    }
}
