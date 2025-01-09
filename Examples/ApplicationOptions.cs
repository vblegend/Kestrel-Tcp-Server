namespace Examples
{
    public class ApplicationOptions
    {
        public ApplicationOptions()
        {
            // 模拟配置文件
            Boolean useTcpProtocol = true;
            if (useTcpProtocol)
            {
                ServerUri = new Uri("tcp://0.0.0.0:50000?readBuffer=1024000&writeBuffer=1024000");
                ClientUri = new Uri("tcp://127.0.0.1:50000?readBuffer=1024000&writeBuffer=1024000");
            }
            else
            {
                ServerUri = new Uri("pipe://.?name=7C795621-301B-2F29-431A-A6A5D25E2D7A&readBuffer=1024000&writeBuffer=1024000");
                ClientUri = new Uri("pipe://127.0.0.1?name=7C795621-301B-2F29-431A-A6A5D25E2D7A");
            }
        }

        public Uri ServerUri { get; set; }

        public Uri ClientUri { get; set; }

    }
}
