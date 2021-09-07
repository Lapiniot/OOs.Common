using System.Net.Connections.Exceptions;
using System.Net.Sockets;
using System.Net.WebSockets;
using static System.Net.Properties.Strings;

namespace System.Net.Connections;

public sealed class WebSocketClientConnection : WebSocketConnection<ClientWebSocket>
{
    private readonly string[] subProtocols;

    public WebSocketClientConnection(Uri remoteUri, string[] subProtocols) : base(null)
    {
        ArgumentNullException.ThrowIfNull(remoteUri);
        ArgumentNullException.ThrowIfNull(subProtocols);

        RemoteUri = remoteUri;
        this.subProtocols = subProtocols;
        if(subProtocols.Length == 0) throw new ArgumentException(NoWsSubProtocol);
    }

    public Uri RemoteUri { get; }

    public IEnumerable<string> SubProtocols => subProtocols;

    #region Overrides of WebSocketConnection

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
            Socket = socket;
        }
        catch(WebSocketException wse) when(wse.InnerException is HttpRequestException
        { InnerException: SocketException { SocketErrorCode: SocketError.HostNotFound } })
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
        return $"{Id}-{nameof(WebSocketClientConnection)}";
    }
}