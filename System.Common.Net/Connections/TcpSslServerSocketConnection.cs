using System.Net.Security;
using System.Net.Sockets;

namespace System.Net.Connections;

public sealed class TcpSslServerSocketConnection : TcpSslSocketConnection
{
    private readonly SslServerAuthenticationOptions options;

    public TcpSslServerSocketConnection(Socket acceptedSocket, SslServerAuthenticationOptions options) :
        base(acceptedSocket) => this.options = options;

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        SslStream = CreateSslStream(Socket);

        try
        {
            await SslStream.AuthenticateAsServerAsync(options, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await using (SslStream.ConfigureAwait(false))
            {
                SslStream = null;
                throw;
            }
        }
    }
}