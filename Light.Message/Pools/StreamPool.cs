using Microsoft.IO;

namespace Light.Message.Pools
{
    public static class StreamPool
    {
        private static RecyclableMemoryStreamManager defaultManager = new RecyclableMemoryStreamManager(PoolOptions());
        static RecyclableMemoryStreamManager.Options PoolOptions()
        {
            var options = new RecyclableMemoryStreamManager.Options()
            {
                BlockSize = 128,
                LargeBufferMultiple = 1024, // 大于该值时使用大缓冲区
                MaximumBufferSize = 8192, //最大缓冲区大小, 超过这个长度的缓冲区不会被池化。
                GenerateCallStacks = false,
                AggressiveBufferReturn = false, // 释放时立即归还内存
                MaximumLargePoolFreeBytes = 1024 * 4096,// 要在大型池中保持可用的最大字节数。
                MaximumSmallPoolFreeBytes = 128 * 4096, // 要在小池中保持可用的最大字节数。
            };

            return options;
        }



        public static RecyclableMemoryStream GetStream()
        {
            return defaultManager.GetStream();
        }
        public static RecyclableMemoryStream GetStream(int requiredSize)
        {
            return defaultManager.GetStream(null, requiredSize);
        }

        public static RecyclableMemoryStream GetContiguousStream(int requiredSize)
        {
            return defaultManager.GetStream(null, requiredSize, true);
        }




    }
}
