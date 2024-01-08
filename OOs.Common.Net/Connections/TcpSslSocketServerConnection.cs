using System.Net.Security;
using System.Net.Sockets;

namespace OOs.Net.Connections;

public sealed class TcpSslSocketServerConnection(Socket acceptedSocket, SslServerAuthenticationOptions options) : TcpSslSocketConnection(acceptedSocket)
{
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