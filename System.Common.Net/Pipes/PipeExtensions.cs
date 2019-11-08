using System.IO.Pipelines;

namespace System.Net.Pipes
{
    public static class PipeExtensions
    {
        public static void Deconstruct(this Pipe pipe, out PipeReader reader, out PipeWriter writer)
        {
            if(pipe == null) throw new ArgumentNullException(nameof(pipe));

            reader = pipe.Reader;
            writer = pipe.Writer;
        }
    }
}