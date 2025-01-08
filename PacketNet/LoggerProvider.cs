using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace PacketNet
{
    public class LoggerProvider
    {
        private static ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

        /// <summary>
        /// 初始化 LoggerFactory
        /// </summary>
        public static void Initialize(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// 获取指定类的日志器
        /// </summary>
        public static ILogger<T> CreateLogger<T>()
        {
            return _loggerFactory.CreateLogger<T>();
        }
    }
}
