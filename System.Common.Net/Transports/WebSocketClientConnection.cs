using System.Net.Http;
using System.Net.Sockets;
using System.Net.Transports.Exceptions;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Properties.Strings;

namespace System.Net.Transports
{
    public class WebSocketClientConnection : WebSocketConnection<ClientWebSocket>
    {
        public WebSocketClientConnection(Uri remoteUri, params string[] subProtocols) : base(null)
        {
            RemoteUri = remoteUri ?? throw new ArgumentNullException(nameof(remoteUri));
            SubProtocols = subProtocols ?? throw new ArgumentNullException(nameof(subProtocols));
            if(SubProtocols.Length == 0) throw new ArgumentException(NoWsSubProtocol);
        }

        public Uri RemoteUri { get; }

        public string[] SubProtocols { get; }

        #region Overrides of WebSocketTransportBase

        public override async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            Socket = new ClientWebSocket();

            foreach(var subProtocol in SubProtocols)
            {
                Socket.Options.AddSubProtocol(subProtocol);
            }

            try
            {
                await Socket.ConnectAsync(RemoteUri, cancellationToken).ConfigureAwait(false);
            }
            catch(WebSocketException wse) when(
                wse.InnerException is HttpRequestException hre &&
                hre.InnerException is SocketException se &&
                se.SocketErrorCode == SocketError.HostNotFound)
            {
                throw new HostNotFoundException(wse);
            }
            catch(WebSocketException wse)
            {
                throw new ServerUnavailableException(wse);
            }
        }

        #endregion

        public override string ToString()
        {
            return $"{nameof(WebSocketClientConnection)}: {(Socket != null ? RemoteUri.ToString() : "Not Connected")}";
        }
    }
}