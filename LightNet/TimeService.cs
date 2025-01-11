using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;

namespace LightNet
{

    /// <summary>
    /// 基于TickCount64的高性能获取当前时间服务，但准确度有限
    /// </summary>
    public class TimeService
    {
        /// <summary>
        /// 默认的时间服务
        /// </summary>
        public static readonly TimeService Default = new TimeService();


        private TimeSpan LocalOffset = TimeZoneInfo.Local.BaseUtcOffset;
        private readonly long _baseTicks;       // 基准 UTC 时间的 Ticks 值
        private readonly long _baseTimestamp;  // 基准时间的 Stopwatch 时间戳
        private readonly double _tickFrequency; // 每个 Tick 的长度 (秒)

        public TimeService()
        {
            // 初始化基准值
            _baseTimestamp = Stopwatch.GetTimestamp(); // 当前 Stopwatch 时间戳
            _baseTicks = DateTime.UtcNow.Ticks; // 当前 UTC 时间的 Ticks
            _tickFrequency = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public UInt64 UtcTicks
        {
            get
            {
                long currentTimestamp = Stopwatch.GetTimestamp();
                long elapsedTicks = (long)((currentTimestamp - _baseTimestamp) * _tickFrequency);
                return (UInt64)(_baseTicks + elapsedTicks);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public UInt64 LocalTicks
        {
            get
            {
                long currentTimestamp = Stopwatch.GetTimestamp();
                long elapsedTicks = (long)((currentTimestamp - _baseTimestamp) * _tickFrequency);
                return (UInt64)(_baseTicks + elapsedTicks + LocalOffset.Ticks);
            }
        }



        /// <summary>
        /// 获取UTC时间
        /// </summary>
        public DateTime UtcNow()
        {
            long currentTimestamp = Stopwatch.GetTimestamp();
            long elapsedTicks = (long)((currentTimestamp - _baseTimestamp) * _tickFrequency);
            return new DateTime(_baseTicks + elapsedTicks, DateTimeKind.Utc);
        }

        /// <summary>
        /// 获取本地时间
        /// </summary>
        public DateTime LocalNow()
        {
            long currentTimestamp = Stopwatch.GetTimestamp();
            long elapsedTicks = (long)((currentTimestamp - _baseTimestamp) * _tickFrequency);
            return new DateTime(_baseTicks + elapsedTicks + LocalOffset.Ticks, DateTimeKind.Local);
        }





        /// <summary>
        /// 获取本地时间
        /// </summary>
        public UInt64 LocalMillisecond
        {
            get
            {
                long currentTimestamp = Stopwatch.GetTimestamp();
                long elapsedTicks = (long)((currentTimestamp - _baseTimestamp) * _tickFrequency);
                return (UInt64)(_baseTicks + elapsedTicks + LocalOffset.Ticks) / TimeSpan.TicksPerMillisecond;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public UInt64 UtcMillisecond
        {
            get
            {
                long currentTimestamp = Stopwatch.GetTimestamp();
                long elapsedTicks = (long)((currentTimestamp - _baseTimestamp) * _tickFrequency);
                return (UInt64)(_baseTicks + elapsedTicks + LocalOffset.Ticks) / TimeSpan.TicksPerMillisecond;
            }
        }



        /// <summary>
        /// 获取本地时间
        /// </summary>
        public UInt64 LocalSecond
        {
            get
            {
                long currentTimestamp = Stopwatch.GetTimestamp();
                long elapsedTicks = (long)((currentTimestamp - _baseTimestamp) * _tickFrequency);
                return (UInt64)(_baseTicks + elapsedTicks + LocalOffset.Ticks) / TimeSpan.TicksPerSecond;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public UInt64 UtcSecond
        {
            get
            {
                long currentTimestamp = Stopwatch.GetTimestamp();
                long elapsedTicks = (long)((currentTimestamp - _baseTimestamp) * _tickFrequency);
                return (UInt64)(_baseTicks + elapsedTicks + LocalOffset.Ticks) / TimeSpan.TicksPerSecond;
            }
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
