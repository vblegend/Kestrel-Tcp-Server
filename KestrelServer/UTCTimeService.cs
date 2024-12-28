using KestrelServer.Message;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KestrelServer
{
    public class UTCTimeService : IHostedService, IDisposable
    {

        private long _baseTicks;       // 基准 UTC 时间的 Ticks 值
        private long _baseTimestamp;  // 基准时间的 Stopwatch 时间戳
        private readonly double _tickFrequency; // 每个 Tick 的长度 (秒)
        private readonly Timer _adjustTimer;    // 定时调整的 Timer
        private static Int32 AdjustIntervalMs = 10000; // 10s 同步一次

        private readonly ILogger<UTCTimeService> logger;
        public UTCTimeService(ILogger<UTCTimeService> _logger)
        {
            this.logger = _logger;
            // 获取每个 Stopwatch Tick 对应的时间 (秒)
            _tickFrequency = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
            // 定期调整基准值的 Timer
            _adjustTimer = new Timer(AdjustBase, null, Timeout.Infinite, Timeout.Infinite);

        }




        /// <summary>
        /// 获取当前 UTC 时间
        /// </summary>
        /// <returns>高效计算的当前 UTC 时间</returns>
        public DateTime Now()
        {
            long baseTicks = Volatile.Read(ref _baseTicks);
            long baseTimestamp = Volatile.Read(ref _baseTimestamp);
            long currentTimestamp = Stopwatch.GetTimestamp();
            long elapsedTicks = (long)((currentTimestamp - baseTimestamp) * _tickFrequency);
            return new DateTime(baseTicks + elapsedTicks, DateTimeKind.Utc);
        }


        /// <summary>
        /// 定期调整基准值以减少漂移
        /// </summary>
        private void AdjustBase(object? state)
        {
            long newBaseTicks = DateTime.UtcNow.Ticks;
            long newBaseTimestamp = Stopwatch.GetTimestamp();
            Volatile.Write(ref _baseTicks, newBaseTicks);
            Volatile.Write(ref _baseTimestamp, newBaseTimestamp);
        }



        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _baseTicks = DateTime.UtcNow.Ticks;
            _baseTimestamp = Stopwatch.GetTimestamp();
            _adjustTimer.Change(0, AdjustIntervalMs);
            await Task.CompletedTask;
        }


        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _adjustTimer.Change(Timeout.Infinite, Timeout.Infinite);
            logger.LogInformation("TimeService.StopAsync()");
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _adjustTimer?.Dispose();
        }
    }
}
