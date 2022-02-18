using System.Net.Connections.Exceptions;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Sockets.SocketError;

namespace System.Net.Connections;

public sealed class TcpSslClientSocketConnection : TcpClientSocketConnection
{
    private readonly X509Certificate[] certificates;
    private readonly SslProtocols enabledSslProtocols;
    private readonly string machineName;
    private SslStream sslStream;

    public TcpSslClientSocketConnection(IPEndPoint endPoint, string machineName,
        SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate[] certificates = null) :
        base(endPoint)
    {
        ArgumentNullException.ThrowIfNull(machineName);

        this.machineName = machineName;
        this.enabledSslProtocols = enabledSslProtocols;
        this.certificates = certificates;
    }

    public TcpSslClientSocketConnection(string hostNameOrAddress, int port,
        string machineName = null, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate[] certificates = null) :
        base(hostNameOrAddress, port)
    {
        this.machineName = machineName ?? hostNameOrAddress;
        this.enabledSslProtocols = enabledSslProtocols;
        this.certificates = certificates;
    }

    public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        CheckState(true);

        try
        {
            await sslStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            await StopActivityAsync().ConfigureAwait(false);
            throw new ConnectionClosedException(se);
        }
    }

    public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        CheckState(true);

        try
        {
            return await sslStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode is ConnectionAborted or ConnectionReset or Shutdown)
        {
            await StopActivityAsync().ConfigureAwait(false);
            throw new ConnectionClosedException(se);
        }
    }

    protected override async Task StartingAsync(CancellationToken cancellationToken)
    {
        await base.StartingAsync(cancellationToken).ConfigureAwait(false);

        var networkStream = new NetworkStream(Socket, FileAccess.ReadWrite, true);

        try
        {
            sslStream = new(networkStream, false);

            try
            {
                var options = new SslClientAuthenticationOptions
                {
                    TargetHost = machineName,
                    EnabledSslProtocols = enabledSslProtocols
                };

                if (certificates is not null)
                {
                    options.ClientCertificates = new(certificates);
                }

                await sslStream.AuthenticateAsClientAsync(options, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await using (sslStream.ConfigureAwait(false))
                {
                    sslStream = null;
                    throw;
                }
            }
        }
        catch
        {
            await using (networkStream.ConfigureAwait(false))
            {
                throw;
            }
        }
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            if (sslStream is not null)
            {
                await sslStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    public override string ToString() => $"{nameof(TcpSslClientSocketConnection)}: {Socket?.RemoteEndPoint?.ToString() ?? "Not connected"}";
}