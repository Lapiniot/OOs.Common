using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

#nullable enable

namespace OOs.Net.Connections;

public sealed class ClientTcpSslSocketTransportConnection : SslSocketTransportConnection
{
    private readonly EndPoint remoteEndPoint;
    private readonly SslClientAuthenticationOptions options;

    public ClientTcpSslSocketTransportConnection(Socket socket,
        EndPoint remoteEndPoint, SslClientAuthenticationOptions options,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
        base(socket, inputPipeOptions, outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(remoteEndPoint);

        this.remoteEndPoint = remoteEndPoint;
        this.options = options;
    }

    public override string ToString() => $"{Id}-TCP.SSL ({RemoteEndPoint?.ToString() ?? "Not connected"})";

    protected override async ValueTask OnStartingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Socket.ConnectAsync(remoteEndPoint, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode == SocketError.HostNotFound)
        {
            ThrowHelper.ThrowHostNotFound(se);
        }
        catch (SocketException se)
        {
            ThrowHelper.ThrowServerUnavailable(se);
        }

        await base.OnStartingAsync(cancellationToken).ConfigureAwait(false);
        await Stream!.AuthenticateAsClientAsync(options, cancellationToken).ConfigureAwait(false);
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
            await Socket.DisconnectAsync(reuseSocket: true).ConfigureAwait(false);
        }
    }

    public static ClientTcpSslSocketTransportConnection Create(IPEndPoint remoteEndPoint,
        string? machineName = null, SslProtocols enabledSslProtocols = SslProtocols.None,
        X509Certificate[]? clientCertificates = null,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) =>
        CreateInternal(remoteEndPoint, machineName, enabledSslProtocols,
            clientCertificates, inputPipeOptions, outputPipeOptions);

    public static ClientTcpSslSocketTransportConnection Create(string host, int port,
        AddressFamily addressFamily = AddressFamily.InterNetwork, string? machineName = null,
        SslProtocols enabledSslProtocols = SslProtocols.None, X509Certificate[]? clientCertificates = null,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) =>
        CreateInternal(new DnsEndPoint(host, port, addressFamily), machineName, enabledSslProtocols,
            clientCertificates, inputPipeOptions, outputPipeOptions);

    private static ClientTcpSslSocketTransportConnection CreateInternal(EndPoint remoteEndPoint, string? machineName,
        SslProtocols enabledSslProtocols, X509Certificate[]? clientCertificates,
        PipeOptions? inputPipeOptions, PipeOptions? outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(remoteEndPoint);

#pragma warning disable CA2000 // Dispose objects before losing scope
        var socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
#pragma warning restore CA2000 // Dispose objects before losing scope

        try
        {
            var options = new SslClientAuthenticationOptions()
            {
                TargetHost = machineName,
                EnabledSslProtocols = enabledSslProtocols,
                ClientCertificates = clientCertificates is { Length: > 0 } ? [.. clientCertificates] : null,
            };
            return new(socket, remoteEndPoint, options, inputPipeOptions, outputPipeOptions);
        }
        catch
        {
            using (socket) throw;
        }
    }
}