namespace Examples
{
    public class ApplicationOptions
    {
        public ApplicationOptions(String protocol)
        {
            if (protocol == "tcp")
            {
                ServerUri = new Uri("tcp://0.0.0.0:50000");
                ClientUri = new Uri("tcp://127.0.0.1:50000");
            }else if (protocol == "pipe")
            {
                ServerUri = new Uri("pipe://.?name=7C795621-301B-2F29-431A-A6A5D25E2D7A");
                ClientUri = new Uri("pipe://192.168.1.20?name=7C795621-301B-2F29-431A-A6A5D25E2D7A");
            }
            else
            {
                throw new Exception("Invalid protocol:" + protocol);
            }
        }




        public Uri ServerUri { get; set; }

        public Uri ClientUri { get; set; }

    }
}
