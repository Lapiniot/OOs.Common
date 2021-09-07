namespace System.IO.Pipelines;

public static class PipeExtensions
{
    public static void Deconstruct(this Pipe pipe, out PipeReader reader, out PipeWriter writer)
    {
        ArgumentNullException.ThrowIfNull(pipe);

        reader = pipe.Reader;
        writer = pipe.Writer;
    }
}