using System.IO.Pipelines;

#pragma warning disable CA1034 // Nested types should not be visible

namespace OOs.IO.Pipelines;

public static class PipeExtensions
{
    extension(Pipe pipe)
    {
        public void Deconstruct(out PipeReader reader, out PipeWriter writer)
        {
            ArgumentNullException.ThrowIfNull(pipe);

            reader = pipe.Reader;
            writer = pipe.Writer;
        }
    }
}