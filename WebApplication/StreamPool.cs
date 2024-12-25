using Microsoft.IO;
using System;
using System.IO;

namespace KestrelServer
{
    public static class StreamPool
    {
        private static RecyclableMemoryStreamManager defaultManager = new RecyclableMemoryStreamManager(PoolOptions());
        static RecyclableMemoryStreamManager.Options PoolOptions()
        {
            var options = new RecyclableMemoryStreamManager.Options()
            {
                BlockSize = 1024,
                LargeBufferMultiple = 1024 * 1024,
                MaximumBufferSize = 16 * 1024 * 1024,
                GenerateCallStacks = false,
                AggressiveBufferReturn = false,
                MaximumLargePoolFreeBytes = 16 * 1024 * 1024 * 4,
                MaximumSmallPoolFreeBytes = 100 * 1024,
            };

            return options;
        }



        public static RecyclableMemoryStream GetStream()
        {
            return defaultManager.GetStream();
        }
        public static RecyclableMemoryStream GetStream(Int32 requiredSize)
        {
            return defaultManager.GetStream(null, requiredSize);
        }

        public static RecyclableMemoryStream GetContiguousStream(Int32 requiredSize)
        {
            return defaultManager.GetStream(null, requiredSize, true);
        }




    }
}
