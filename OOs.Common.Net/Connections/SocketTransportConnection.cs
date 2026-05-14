using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;

namespace OOs.Net.Connections;

public abstract class SocketTransportConnection(Socket socket, PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
    SocketTransportConnectionBase(socket, inputPipeOptions, outputPipeOptions)
{
    private readonly SocketReceiverAsyncEventArgs receiveArgs = new((inputPipeOptions ?? DefaultInputPipeOptions).WriterScheduler);
    private readonly SocketSenderAsyncEventArgs sendArgs = new((outputPipeOptions ?? DefaultOutputPipeOptions).ReaderScheduler);
    private readonly MultiBufferSocketSenderAsyncEventArgs multiBufferSendArgs = new((outputPipeOptions ?? DefaultOutputPipeOptions).ReaderScheduler);

    protected sealed override ValueTask<int> ReceiveAsync(Memory<byte> buffer)
    {
        return receiveArgs.ReceiveAsync(Socket, buffer);
    }

    protected sealed override ValueTask SendAsync(ref readonly ReadOnlySequence<byte> buffer)
    {
        return buffer.IsSingleSegment
            ? sendArgs.SendAsync(Socket, buffer.First)
            : multiBufferSendArgs.SendAsync(Socket, in buffer);
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        try
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            receiveArgs.Dispose();
            sendArgs.Dispose();
            multiBufferSendArgs.Dispose();
        }
    }

    private class AwaitableSocketAsyncEventArgs(PipeScheduler scheduler) : SocketAsyncEventArgs, IValueTaskSource
    {
        private static readonly Action<object?> completed = _ => { };
        private volatile Action<object?>? continuation;
        private readonly PipeScheduler scheduler = scheduler;

        public void GetResult(short token)
        {
            continuation = null;

            if (SocketError is not SocketError.Success and var socketError)
            {
                ThrowSocketException(socketError);
            }
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

        protected sealed override void OnCompleted(SocketAsyncEventArgs e)
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

    private sealed class SocketReceiverAsyncEventArgs(PipeScheduler scheduler) : AwaitableSocketAsyncEventArgs(scheduler), IValueTaskSource<int>
    {
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

        int IValueTaskSource<int>.GetResult(short token)
        {
            GetResult(token);
            return BytesTransferred;
        }
    }

    private sealed class SocketSenderAsyncEventArgs(PipeScheduler scheduler) : AwaitableSocketAsyncEventArgs(scheduler)
    {
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
    }

    private sealed class MultiBufferSocketSenderAsyncEventArgs(PipeScheduler scheduler) : AwaitableSocketAsyncEventArgs(scheduler)
    {
        private List<ArraySegment<byte>>? buffers;
        public ValueTask SendAsync(Socket socket, ref readonly ReadOnlySequence<byte> buffer)
        {
            buffers ??= [];
            buffers.Clear();

            var position = buffer.Start;
            while (buffer.TryGet(ref position, out var memory))
            {
                if (!MemoryMarshal.TryGetArray(memory, out var segment))
                {
                    ThrowNotAnArrayBackedMemory();
                }

                buffers.Add(segment);
            }

            BufferList = buffers;

            if (socket.SendAsync(this))
            {
                return new ValueTask(this, 0);
            }

            // Completed synchronously
            return SocketError is SocketError.Success
                ? ValueTask.CompletedTask
                : ValueTask.FromException(new SocketException((int)SocketError));
        }

        [DoesNotReturn]
        private static void ThrowNotAnArrayBackedMemory() =>
            throw new InvalidOperationException("Memory is not backed by an array.");
    }
}