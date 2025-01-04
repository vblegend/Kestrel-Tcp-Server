using Microsoft.Extensions.DependencyInjection;
using System;

namespace KestrelServer
{

    /// <summary>
    /// 基于TickCount64的高性能获取当前时间服务，但准确度有限
    /// </summary>
    public class TimeService
    {
        private TimeSpan LocalOffset = TimeZoneInfo.Local.BaseUtcOffset;
        private readonly long _baseTicks;       // 基准 UTC 时间的 Ticks 值
        private readonly long _baseTickCount;  // 基准的 TickCount64 值

        public TimeService()
        {
            _baseTicks = DateTime.UtcNow.Ticks;
            _baseTickCount = Environment.TickCount64;
        }

        /// <summary>
        /// 获取UTC时间
        /// </summary>
        public DateTime UtcNow()
        {
            long elapsedMilliseconds = Environment.TickCount64 - _baseTickCount;
            long elapsedTicks = elapsedMilliseconds * TimeSpan.TicksPerMillisecond;
            return new DateTime(_baseTicks + elapsedTicks, DateTimeKind.Utc);
        }

        /// <summary>
        /// 获取本地时间
        /// </summary>
        public DateTime Now()
        {
            long elapsedMilliseconds = Environment.TickCount64 - _baseTickCount;
            long elapsedTicks = elapsedMilliseconds * TimeSpan.TicksPerMillisecond;
            return new DateTime(_baseTicks + elapsedTicks + LocalOffset.Ticks, DateTimeKind.Local);
        }
    }






    public static class TimeServiceExtension
    {
        public static IServiceCollection AddTimeService(this IServiceCollection collection)
        {
            collection.AddSingleton<TimeService>();
            return collection;
        }
    }






}
