using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

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

    protected override async ValueTask OnStartingAsync()
    {
        try
        {
            await Socket.ConnectAsync(remoteEndPoint).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode == SocketError.HostNotFound)
        {
            ThrowHelper.ThrowHostNotFound(se);
        }
        catch (SocketException se)
        {
            ThrowHelper.ThrowServerUnavailable(se);
        }

        await base.OnStartingAsync().ConfigureAwait(false);
        await Stream!.AuthenticateAsClientAsync(options).ConfigureAwait(false);
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
        SslClientAuthenticationOptions? clientAuthenticationOptions = null,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null)
    {
        ArgumentNullException.ThrowIfNull(remoteEndPoint);

        return CreateCore(remoteEndPoint, clientAuthenticationOptions ?? new()
        {
            TargetHost = remoteEndPoint.Address.ToString()
        }, inputPipeOptions, outputPipeOptions);
    }

    public static ClientTcpSslSocketTransportConnection Create(DnsEndPoint remoteEndPoint,
        SslClientAuthenticationOptions? clientAuthenticationOptions = null,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null)
    {
        ArgumentNullException.ThrowIfNull(remoteEndPoint);

        return CreateCore(remoteEndPoint, clientAuthenticationOptions ?? new()
        {
            TargetHost = remoteEndPoint.Host
        }, inputPipeOptions, outputPipeOptions);
    }

    private static ClientTcpSslSocketTransportConnection CreateCore(EndPoint remoteEndPoint,
        SslClientAuthenticationOptions clientAuthenticationOptions,
        PipeOptions? inputPipeOptions, PipeOptions? outputPipeOptions)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
#pragma warning restore CA2000 // Dispose objects before losing scope

        try
        {
            return new(socket, remoteEndPoint, clientAuthenticationOptions, inputPipeOptions, outputPipeOptions);
        }
        catch
        {
            using (socket) throw;
        }
    }
}