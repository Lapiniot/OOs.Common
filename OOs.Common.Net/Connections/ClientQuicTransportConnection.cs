using System.IO.Pipelines;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Versioning;

#nullable enable

namespace OOs.Net.Connections;
#if !NET9_0_OR_GREATER
[RequiresPreviewFeatures]
#endif
[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("osx")]
public sealed class ClientQuicTransportConnection : QuicTransportConnection
{
    private readonly QuicClientConnectionOptions options;

    public ClientQuicTransportConnection(QuicClientConnectionOptions options,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
        base(inputPipeOptions, outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options;
    }

    public override string ToString() => $"{Id}-QUIC ({RemoteEndPoint?.ToString() ?? "Not connected"})";

    protected override async ValueTask OnStartingAsync()
    {
        try
        {
            Connection = await QuicConnection.ConnectAsync(options).ConfigureAwait(false);
            Stream = await Connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, default).ConfigureAwait(false);
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
        SslApplicationProtocol protocol, string? targetHost = null,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) =>
        CreateInternal(remoteEndPoint, protocol, targetHost ?? remoteEndPoint?.Address.ToString(),
            inputPipeOptions, outputPipeOptions);

    public static ClientQuicTransportConnection Create(DnsEndPoint remoteEndPoint,
        SslApplicationProtocol protocol, string? targetHost = null,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) =>
        CreateInternal(remoteEndPoint, protocol, targetHost ?? remoteEndPoint?.Host,
            inputPipeOptions, outputPipeOptions);

    private static ClientQuicTransportConnection CreateInternal(EndPoint remoteEndPoint,
        SslApplicationProtocol protocol, string? targetHost,
        PipeOptions? inputPipeOptions, PipeOptions? outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(remoteEndPoint);

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
            ClientAuthenticationOptions = new()
            {
                ApplicationProtocols = [protocol],
                TargetHost = targetHost
            }
        };

        return new(options, inputPipeOptions, outputPipeOptions);
    }
}