using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    /// <summary>
    /// Represents server web-socket connection with additional feature: disconnection signaling
    /// via provided external <see cref="TaskCompletionSource{TResult}" /> completion source
    /// </summary>
    public class WebSocketServerConnectionWithSignaling : WebSocketConnection<WebSocket>
    {
        private readonly IPEndPoint remoteEndPoint;
        private readonly TaskCompletionSource<bool> taskCompletionSource;

        public WebSocketServerConnectionWithSignaling(WebSocket socket, IPEndPoint remoteEndPoint, TaskCompletionSource<bool> completionSource) : base(socket)
        {
            this.remoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
            taskCompletionSource = completionSource ?? throw new ArgumentNullException(nameof(completionSource));
        }

        public override string ToString()
        {
            return $"{nameof(WebSocketServerConnectionWithSignaling)}: {remoteEndPoint}";
        }

        #region Overrides of WebSocketConnection<WebSocket>

        public override Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public override async Task DisconnectAsync()
        {
            try
            {
                await base.DisconnectAsync().ConfigureAwait(false);
                taskCompletionSource.TrySetResult(true);
            }
            catch(Exception exception)
            {
                taskCompletionSource.TrySetException(exception);
                throw;
            }
        }

        #endregion
    }
}