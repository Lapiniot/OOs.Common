using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    public static class SocketExtensions
    {
        public delegate IAsyncResult AsyncBeginHandler(byte[] bytes, int offset, int size, SocketFlags flags, AsyncCallback callback, object state);

        public delegate T AsyncEndHandler<out T>(IAsyncResult asyncResult);

        public static async Task<T> FromAsync<T>(Socket socket, byte[] bytes, int offset, int size, IPEndPoint remotEndPoint,
            AsyncBeginHandler beginMethod, AsyncEndHandler<T> endMethod, CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<T>();

            using(completionSource.Bind(cancellationToken))
            {
                try
                {
                    var asyncState = new AsyncStateBag<T>(socket, remotEndPoint, completionSource, endMethod);

                    beginMethod(bytes, offset, size, SocketFlags.None, asyncState.AsyncCallback, asyncState);
                }
                catch(Exception exception)
                {
                    completionSource.TrySetException(exception);
                }

                return await completionSource.Task.ConfigureAwait(false);
            }
        }

        private class AsyncStateBag<T>
        {
            public AsyncStateBag(Socket socket, IPEndPoint endPoint, TaskCompletionSource<T> completionSource, AsyncEndHandler<T> endMethod) =>
                (Socket, EndPoint, CompletionSource, EndMethod) = (socket, endPoint, completionSource, endMethod);

            private AsyncEndHandler<T> EndMethod { get; }
            private TaskCompletionSource<T> CompletionSource { get; }
            public Socket Socket { get; }
            public IPEndPoint EndPoint { get; }

            public void AsyncCallback(IAsyncResult ar)
            {
                try
                {
                    CompletionSource.TrySetResult(EndMethod(ar));
                }
                catch(Exception exception)
                {
                    CompletionSource.TrySetException(exception);
                }
            }
        }

        #region SendAsync overloads

        public static Task<int> SendAsync(this Socket socket, byte[] bytes, int offset, int size, CancellationToken cancellationToken)
        {
            return FromAsync(socket, bytes, offset, size, null, socket.BeginSend, socket.EndSend, cancellationToken);
        }

        public static Task<int> SendAsync(this Socket socket, byte[] bytes, CancellationToken cancellationToken)
        {
            return SendAsync(socket, bytes, 0, bytes.Length, cancellationToken);
        }

        #endregion

        #region SendToAsync overloads

        public static Task<int> SendToAsync(this Socket socket, byte[] bytes, int offset, int size, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            return FromAsync(socket, bytes, offset, size, remoteEndPoint, BeginSendTo, EndSendTo, cancellationToken);
        }

        public static Task<int> SendToAsync(this Socket socket, byte[] bytes, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            return SendToAsync(socket, bytes, 0, bytes.Length, remoteEndPoint, cancellationToken);
        }

        public static Task<int> SendToAsync(this Socket socket, Memory<byte> memory, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            if(MemoryMarshal.TryGetArray<byte>(memory, out var segment))
            {
                return FromAsync(socket, segment.Array, segment.Offset, segment.Count, remoteEndPoint, BeginSendTo, EndSendTo, cancellationToken);
            }
            else
            {
                using(var owner = MemoryPool<byte>.Shared.Rent(memory.Length))
                {
                    memory.CopyTo(owner.Memory);
                    MemoryMarshal.TryGetArray<byte>(owner.Memory, out segment);
                    return FromAsync(socket, segment.Array, 0, memory.Length, remoteEndPoint, BeginSendTo, EndSendTo, cancellationToken);
                }
            }
        }

        private static IAsyncResult BeginSendTo(byte[] bytes, int offset, int size, SocketFlags flags, AsyncCallback callback, object state)
        {
            var asyncState = (AsyncStateBag<int>)state;

            return asyncState.Socket.BeginSendTo(bytes, offset, size, flags, asyncState.EndPoint, callback, state);
        }

        private static int EndSendTo(IAsyncResult asyncResult)
        {
            return ((AsyncStateBag<int>)asyncResult.AsyncState).Socket.EndSendTo(asyncResult);
        }

        #endregion

        #region ReceiveAsync overloads

        public static Task<int> ReceiveAsync(this Socket socket, byte[] bytes, int offset, int size, CancellationToken cancellationToken)
        {
            return FromAsync(socket, bytes, offset, size, null, socket.BeginReceive, socket.EndReceive, cancellationToken);
        }

        public static Task<int> ReceiveAsync(this Socket socket, byte[] bytes, CancellationToken cancellationToken)
        {
            return ReceiveAsync(socket, bytes, 0, bytes.Length, cancellationToken);
        }

        #endregion

        #region ReceiveFromAsync overloads 

        public static Task<(int Size, IPEndPoint RemoteEndPoint)> ReceiveFromAsync(this Socket socket, byte[] bytes, int offset, int size,
            IPEndPoint endPoint, CancellationToken cancellationToken)
        {
            return FromAsync(socket, bytes, offset, size, endPoint, BeginReceiveFrom, EndReceiveFrom, cancellationToken);
        }

        public static Task<(int Size, IPEndPoint RemoteEndPoint)> ReceiveFromAsync(this Socket socket, byte[] bytes, IPEndPoint endPoint, CancellationToken cancellationToken)
        {
            return ReceiveFromAsync(socket, bytes, 0, bytes.Length, endPoint, cancellationToken);
        }

        private static IAsyncResult BeginReceiveFrom(byte[] bytes, int offset, int size, SocketFlags flags, AsyncCallback callback, object state)
        {
            var asyncState = (AsyncStateBag<(int, IPEndPoint)>)state;
            EndPoint e = asyncState.EndPoint;
            return asyncState.Socket.BeginReceiveFrom(bytes, offset, size, flags, ref e, callback, state);
        }

        private static (int Size, IPEndPoint RemoteEndPoint) EndReceiveFrom(IAsyncResult result)
        {
            var asyncState = (AsyncStateBag<(int, IPEndPoint)>)result.AsyncState;
            EndPoint endPoint = asyncState.EndPoint;
            return (asyncState.Socket.EndReceiveFrom(result, ref endPoint), (IPEndPoint)endPoint);
        }

        #endregion
    }
}