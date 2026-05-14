using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;

namespace OOs.Net.Connections;

public abstract class SocketTransportConnection(Socket socket, PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
    SocketTransportConnectionBase(socket, inputPipeOptions, outputPipeOptions)
{
    private readonly AwaitableSocketAsyncEventArgs receiveArgs = new((inputPipeOptions ?? DefaultInputPipeOptions).WriterScheduler);
    private readonly AwaitableSocketAsyncEventArgs sendArgs = new((outputPipeOptions ?? DefaultOutputPipeOptions).ReaderScheduler);

    protected override ValueTask<int> ReceiveAsync(Memory<byte> buffer)
    {
        return receiveArgs.ReceiveAsync(Socket, buffer);
    }

    protected override ValueTask SendAsync(ReadOnlyMemory<byte> buffer)
    {
        return sendArgs.SendAsync(Socket, buffer);
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        using (receiveArgs)
        using (sendArgs)
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class AwaitableSocketAsyncEventArgs(PipeScheduler scheduler) : SocketAsyncEventArgs,
        IValueTaskSource, IValueTaskSource<int>
    {
        private static readonly Action<object?> completed = _ => { };
        private volatile Action<object?>? continuation;
        private readonly PipeScheduler scheduler = scheduler;

        public ValueTask SendAsync(Socket socket, ReadOnlyMemory<byte> buffer)
        {
            SetBuffer(MemoryMarshal.AsMemory(buffer));

            if (socket.SendAsync(this))
            {
                return new ValueTask(this, 0);
            }

            // Completed synchronously
            return SocketError is SocketError.Success
                ? ValueTask.CompletedTask
                : ValueTask.FromException(new SocketException((int)SocketError));
        }

        public ValueTask<int> ReceiveAsync(Socket socket, Memory<byte> buffer)
        {
            SetBuffer(buffer);

            if (socket.ReceiveAsync(this))
            {
                return new ValueTask<int>(this, 0);
            }

            // Completed synchronously
            return SocketError is SocketError.Success
                ? ValueTask.FromResult(BytesTransferred)
                : ValueTask.FromException<int>(new SocketException((int)SocketError));
        }

        public void GetResult(short token)
        {
            continuation = null;

            if (SocketError is not SocketError.Success and var socketError)
            {
                ThrowSocketException(socketError);
            }
        }

        int IValueTaskSource<int>.GetResult(short token)
        {
            continuation = null;

            if (SocketError is not SocketError.Success and var socketError)
            {
                ThrowSocketException(socketError);
            }

            return BytesTransferred;
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            return !ReferenceEquals(continuation, completed)
                ? ValueTaskSourceStatus.Pending
                : SocketError is SocketError.Success
                    ? ValueTaskSourceStatus.Succeeded
                    : ValueTaskSourceStatus.Faulted;
        }

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            UserToken = state;
            var current = Interlocked.CompareExchange(ref this.continuation, continuation, null);
            if (ReferenceEquals(current, completed))
            {
                UserToken = null;
                scheduler.Schedule(continuation, state);
            }
        }

        protected override void OnCompleted(SocketAsyncEventArgs e)
        {
            var current = continuation;
            if (current != null || (current = Interlocked.CompareExchange(ref continuation, completed, null)) != null)
            {
                // Mark as already completed, so GetStatus() correctly returns one of the completed statuses.
                continuation = completed;

                var state = UserToken;
                UserToken = null;

                scheduler.Schedule(current, state);
            }
        }

        [DoesNotReturn]
        private static void ThrowSocketException(SocketError socketError) =>
            throw new SocketException((int)socketError);
    }
}