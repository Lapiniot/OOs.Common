using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines
{
    public class StreamPipeReader : PipeReaderBase
    {
        private readonly Stream stream;

        public StreamPipeReader(Stream stream, PipeOptions pipeOptions = null) : base(pipeOptions)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        protected override ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return stream.ReadAsync(buffer, cancellationToken);
        }
    }
}