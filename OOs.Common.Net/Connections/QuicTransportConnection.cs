using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Quic;
using System.Runtime.Versioning;

namespace OOs.Net.Connections;

#pragma warning disable CA1416 // Validate platform compatibility

[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("osx")]
public abstract class QuicTransportConnection : TransportConnectionPipeAdapter
{
    private QuicStream? stream;
    private QuicConnection? connection;

    protected QuicTransportConnection(QuicConnection connection,
        PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
        base(inputPipeOptions, outputPipeOptions)
    {
        ArgumentNullException.ThrowIfNull(connection);
        this.connection = connection;
    }

    protected QuicTransportConnection(PipeOptions? inputPipeOptions = null, PipeOptions? outputPipeOptions = null) :
        base(inputPipeOptions, outputPipeOptions)
    {
    }

    /// <summary>
    /// Returns <c>true</c> if QUIC is supported on the current machine and can be used; otherwise, <c>false</c>.
    /// </summary>
    /// <remarks>
    /// The current implementation depends on <see href="https://github.com/microsoft/msquic">MsQuic</see> native library, 
    /// this property checks its presence (Linux machines).
    /// It also checks whether TLS 1.3, requirement for QUIC protocol, is available and enabled (Windows machines).
    /// </remarks>
    [SupportedOSPlatformGuard("windows")]
    [SupportedOSPlatformGuard("linux")]
    [SupportedOSPlatformGuard("osx")]
    public static bool IsSupported => QuicConnection.IsSupported;

    public sealed override string Id { get; } = Base32.ToBase32String(CorrelationIdGenerator.GetNext());
    public override EndPoint? LocalEndPoint => connection?.LocalEndPoint;
    public override EndPoint? RemoteEndPoint => connection?.RemoteEndPoint;

    protected QuicStream? Stream { get => stream; set => stream = value; }
    protected QuicConnection? Connection { get => connection; set => connection = value; }

    public override void Abort()
    {
        stream?.Abort(QuicAbortDirection.Both, 0x00);
        base.Abort();
    }

    protected override ValueTask ShutdownAsync(ShutdownDirection direction)
    {
        if (stream is null)
        {
            return default;
        }

        switch (direction)
        {
            case ShutdownDirection.Send:
                stream.CompleteWrites();
                return new ValueTask(stream.WritesClosed);
            case ShutdownDirection.Receive:
                stream.Abort(QuicAbortDirection.Read, 0x00);
                return default;
            case ShutdownDirection.Both:
                stream.Abort(QuicAbortDirection.Both, 0x00);
                return default;
            default:
                ThrowInvalidShutdownDirection<QuicAbortDirection>(direction);
                return default;
        }
    }

    protected override async ValueTask OnStoppingAsync()
    {
        await using (connection)
        await using (stream)
        {
            stream!.CompleteWrites();
            await stream.WritesClosed.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            await connection!.CloseAsync(0x00).ConfigureAwait(false);
        }
    }

    protected sealed override ValueTask<int> ReceiveAsync(Memory<byte> buffer) => stream!.ReadAsync(buffer);

    protected sealed override ValueTask SendAsync(ref readonly ReadOnlySequence<byte> buffer)
    {
        return buffer.IsSingleSegment ? stream!.WriteAsync(buffer.First) : SendAsync(buffer);
    }

    private async ValueTask SendAsync(ReadOnlySequence<byte> buffer)
    {
        var position = buffer.Start;
        while (buffer.TryGet(ref position, out var memory))
        {
            await stream!.WriteAsync(memory).ConfigureAwait(false);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        await using (connection)
        await using (stream)
        {
            await base.DisposeAsync().AsTask().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}