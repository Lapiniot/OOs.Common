using System.Collections.Generic;
using System.Net.Connections.Exceptions;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Properties.Strings;

namespace System.Net.Connections
{
    public sealed class WebSocketClientConnection : WebSocketConnection<ClientWebSocket>
    {
        private readonly string[] subProtocols;

        public WebSocketClientConnection(Uri remoteUri, params string[] subProtocols) : base(null)
        {
            RemoteUri = remoteUri ?? throw new ArgumentNullException(nameof(remoteUri));
            this.subProtocols = subProtocols ?? throw new ArgumentNullException(nameof(subProtocols));
            if(subProtocols.Length == 0) throw new ArgumentException(NoWsSubProtocol);
        }

        public Uri RemoteUri { get; }

        public IEnumerable<string> SubProtocols => subProtocols;

        #region Overrides of WebSocketTransportBase

        public override async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            var socket = new ClientWebSocket();

            foreach(var subProtocol in SubProtocols)
            {
                socket.Options.AddSubProtocol(subProtocol);
            }

            try
            {
                await socket.ConnectAsync(RemoteUri, cancellationToken).ConfigureAwait(false);
                SetWebSocket(socket);
            }
            catch(WebSocketException wse) when(wse.InnerException is HttpRequestException
                {InnerException: SocketException {SocketErrorCode: SocketError.HostNotFound}})
            {
                throw new HostNotFoundException(wse);
            }
            catch(WebSocketException wse)
            {
                throw new ServerUnavailableException(wse);
            }
        }

        #endregion
    }
}