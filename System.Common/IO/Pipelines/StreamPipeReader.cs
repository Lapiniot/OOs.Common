namespace System.IO.Pipelines;

public class StreamPipeReader : PipeReaderBase
{
    private readonly Stream stream;

    public StreamPipeReader(Stream stream, PipeOptions pipeOptions = null) : base(pipeOptions)
    {
        ArgumentNullException.ThrowIfNull(stream);
        this.stream = stream;
    }

    protected override ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        return stream.ReadAsync(buffer, cancellationToken);
    }
}