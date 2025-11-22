using System.IO.Pipelines;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Versioning;

#nullable enable

namespace OOs.Net.Connections;

[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("osx")]
public sealed class ClientQuicTransportConnection : QuicTransportConnection
{
    private readonly QuicClientConnectionOptions options;

    public static ReadOnlySpan<SslApplicationProtocol> DefaultSslApplicationProtocols => new[] {
        new("mqtt-quic"),
        SslApplicationProtocol.Http3
    };

    public ClientQuicTransportConnection(QuicClientConnectionOptions options,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
        base(inputPipeOptions, outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options;
    }

    public override string ToString() => $"{Id}-QUIC ({RemoteEndPoint?.ToString() ?? "Not connected"})";

    protected override async ValueTask OnStartingAsync(CancellationToken cancellationToken)
    {
        try
        {
            Connection = await QuicConnection.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
            Stream = await Connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException se) when (se.SocketErrorCode == SocketError.HostNotFound)
        {
            ThrowHelper.ThrowHostNotFound(se);
        }
        catch (SocketException se)
        {
            ThrowHelper.ThrowServerUnavailable(se);
        }
    }

    public static ClientQuicTransportConnection Create(IPEndPoint remoteEndPoint,
        SslClientAuthenticationOptions? clientAuthenticationOptions = null,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null)
    {
        ArgumentNullException.ThrowIfNull(remoteEndPoint);

        return CreateCore(remoteEndPoint,
            clientAuthenticationOptions ?? CreateDefaultOptions(remoteEndPoint.Address.ToString()),
            inputPipeOptions, outputPipeOptions);
    }

    public static ClientQuicTransportConnection Create(DnsEndPoint remoteEndPoint,
        SslClientAuthenticationOptions? clientAuthenticationOptions = null,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null)
    {
        ArgumentNullException.ThrowIfNull(remoteEndPoint);

        return CreateCore(remoteEndPoint,
            clientAuthenticationOptions ?? CreateDefaultOptions(remoteEndPoint.Host),
            inputPipeOptions, outputPipeOptions);
    }

    private static SslClientAuthenticationOptions CreateDefaultOptions(string targetHost)
    {
        return new()
        {
            TargetHost = targetHost,
            ApplicationProtocols = [.. DefaultSslApplicationProtocols]
        };
    }

    private static ClientQuicTransportConnection CreateCore(EndPoint remoteEndPoint,
        SslClientAuthenticationOptions clientAuthenticationOptions,
        PipeOptions? inputPipeOptions, PipeOptions? outputPipeOptions)
    {
        QuicClientConnectionOptions options = new()
        {
            IdleTimeout = Timeout.InfiniteTimeSpan,
            RemoteEndPoint = remoteEndPoint,
            // Used to abort stream if it's not properly closed by the user.
            // See https://www.rfc-editor.org/rfc/rfc9000#section-20.2
            DefaultStreamErrorCode = 0x0A, // Protocol-dependent error code.
            // Used to close the connection if it's not done by the user.
            // See https://www.rfc-editor.org/rfc/rfc9000#section-20.2
            DefaultCloseErrorCode = 0x0B, // Protocol-dependent error code.
            ClientAuthenticationOptions = clientAuthenticationOptions
        };

        return new(options, inputPipeOptions, outputPipeOptions);
    }
}