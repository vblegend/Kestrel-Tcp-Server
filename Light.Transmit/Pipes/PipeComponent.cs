
using System.IO.Pipes;

namespace Light.Transmit.Pipes
{
    internal class PipeComponent
    {
        public const PipeOptions DEFAULT_PIPE_OPTIONS = PipeOptions.Asynchronous | PipeOptions.WriteThrough;
    }
}
