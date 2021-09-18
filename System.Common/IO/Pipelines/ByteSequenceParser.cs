using System.Buffers;

namespace System.IO.Pipelines;

public abstract class ByteSequenceParser<T> : IAsyncEnumerable<T>
{
    private readonly PipeReader reader;

    protected ByteSequenceParser(PipeReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        this.reader = reader;
    }

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while(!cancellationToken.IsCancellationRequested)
        {
            var rt = reader.ReadAsync(cancellationToken);

            ReadResult result;

            try
            {
                result = rt.IsCompletedSuccessfully ? rt.Result : await rt.ConfigureAwait(false);
            }
            catch(OperationCanceledException)
            {
                yield break;
            }

            var buffer = result.Buffer;

            if(buffer.Length > 0)
            {
                var consumed = 0L;
                foreach(var (count, data, completed) in Parse(result))
                {
                    consumed += count;
                    if(count > 0) yield return data;
                    if(completed || cancellationToken.IsCancellationRequested)
                    {
                        Advance(buffer, consumed);
                        break;
                    }
                }
                Advance(buffer, consumed);
            }

            if(result.IsCompleted || result.IsCanceled) yield break;
        }
    }

    private void Advance(ReadOnlySequence<byte> buffer, long offset)
    {
        if(offset > 0)
        {
            reader.AdvanceTo(buffer.GetPosition(offset));
        }
        else
        {
            reader.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    protected abstract IEnumerable<ParseResult> Parse(ReadResult readResult);

    protected record ParseResult(long Consumed, T Result, bool IsCompleted);
}