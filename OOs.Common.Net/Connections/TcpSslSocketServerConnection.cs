using System.Net.Security;
using System.Net.Sockets;

namespace OOs.Net.Connections;

[Obsolete("Consider usage of OOs.Net.Connections.ServerTcpSslSocketTransportConnection instead.")]
public sealed class TcpSslSocketServerConnection(Socket acceptedSocket, SslServerAuthenticationOptions options) :
    TcpSslSocketConnection(acceptedSocket, reuseSocket: false)
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

    protected override Task StoppingAsync() => base.StoppingAsync();

}