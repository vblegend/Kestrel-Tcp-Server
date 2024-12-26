using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KestrelServer
{
    public class TimeService : IHostedService
    {

        private DateTime sTime;
        private Stopwatch stopwatch;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            stopwatch = Stopwatch.StartNew();
            var utcTime = DateTime.UtcNow;
            sTime = new DateTime(utcTime.Year, utcTime.Month, utcTime.Day, utcTime.Hour, utcTime.Minute, utcTime.Second, DateTimeKind.Utc);
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            stopwatch.Stop();
            await Task.CompletedTask;
        }


        public DateTime BaseTime()
        {
            return this.sTime;
        }


        public DateTime Now()
        {
            return this.sTime.AddMilliseconds(stopwatch.ElapsedMilliseconds);
        }


        public UInt32 Tick()
        {
            return (UInt32)stopwatch.ElapsedMilliseconds;
        }



    }
}
