namespace Examples
{
    public class ApplicationOptions
    {
        public ApplicationOptions()
        {
            // 模拟配置文件
            Boolean useTcpProtocol = false;
            // 提升发送接收缓冲区大小可以提升高吞吐量
            var readBufferSize = 1024 * 8;
            var writeBufferSize = 1024 * 8;
            if (useTcpProtocol)
            {
                ServerUri = new Uri($"tcp://0.0.0.0:50000?readBuffer={readBufferSize}&writeBuffer={writeBufferSize}");
                ClientUri = new Uri($"tcp://127.0.0.1:50000?readBuffer={readBufferSize}&writeBuffer={writeBufferSize}");
            }
            else
            {
                ServerUri = new Uri($"pipe://.?name=7C795621-301B-2F29-431A-A6A5D25E2D7A&readBuffer={readBufferSize}&writeBuffer={writeBufferSize}");
                ClientUri = new Uri($"pipe://127.0.0.1?name=7C795621-301B-2F29-431A-A6A5D25E2D7A&readBuffer={readBufferSize}&writeBuffer={writeBufferSize}");
            }
        }

        public Uri ServerUri { get; set; }

        public Uri ClientUri { get; set; }

    }
}
