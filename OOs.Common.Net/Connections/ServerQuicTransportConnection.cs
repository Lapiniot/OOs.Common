using System.IO.Pipelines;
using System.Net.Quic;
using System.Runtime.Versioning;

namespace OOs.Net.Connections;

[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("osx")]
public sealed class ServerQuicTransportConnection(QuicConnection connection,
    PipeOptions inputPipeOptions = null, PipeOptions outputPipeOptions = null) :
    QuicTransportConnection(connection, inputPipeOptions, outputPipeOptions)
{
    public override string ToString() => $"{Id}-QUIC ({RemoteEndPoint})";

    protected override async ValueTask OnStartingAsync(CancellationToken cancellationToken)
    {
        Stream = await Connection.AcceptInboundStreamAsync(cancellationToken).ConfigureAwait(false);
    }
}