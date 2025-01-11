
using System.IO.Pipes;

namespace LightNet.Pipes
{
    internal class PipeComponent
    {
        public const PipeOptions DEFAULT_PIPE_OPTIONS = PipeOptions.Asynchronous | PipeOptions.WriteThrough;
    }
}
