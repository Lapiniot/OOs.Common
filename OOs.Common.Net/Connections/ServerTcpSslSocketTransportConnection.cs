using System.IO.Pipelines;
using System.Net.Security;
using System.Net.Sockets;

#nullable enable

namespace OOs.Net.Connections;

public sealed class ServerTcpSslSocketTransportConnection : SslSocketTransportConnection
{
    private readonly SslServerAuthenticationOptions options;

    public ServerTcpSslSocketTransportConnection(Socket acceptedSocket, SslServerAuthenticationOptions options,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
        base(acceptedSocket, inputPipeOptions, outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
    }

    public override string ToString() => $"{Id}-TCP.SSL ({RemoteEndPoint})";

    protected override async ValueTask OnStartingAsync()
    {
        await base.OnStartingAsync().ConfigureAwait(false);
        await Stream!.AuthenticateAsServerAsync(options).ConfigureAwait(false);
    }

    protected override async ValueTask OnStoppingAsync()
    {
        try
        {
            await base.OnStoppingAsync().ConfigureAwait(false);
        }
        finally
        {
            Socket.Shutdown(SocketShutdown.Both);
            await Socket.DisconnectAsync(reuseSocket: false).ConfigureAwait(false);
        }
    }
}