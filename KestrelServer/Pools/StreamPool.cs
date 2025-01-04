using Microsoft.IO;

namespace KestrelServer.Pools
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
