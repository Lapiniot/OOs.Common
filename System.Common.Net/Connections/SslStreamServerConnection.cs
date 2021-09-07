using System.Net.Security;
using System.Net.Sockets;

namespace System.Net.Connections;

public sealed class SslStreamServerConnection : NetworkConnection
{
#pragma warning disable CA2213 // Disposable fields should be disposed: Warning is wrongly emitted due to some issues with analyzer itself
    private SslStream sslStream;
#pragma warning restore CA2213
    private Socket socket;
    private readonly SslServerAuthenticationOptions options;

    public SslStreamServerConnection(Socket acceptedSocket, SslServerAuthenticationOptions options)
    {
        this.socket = acceptedSocket;
        this.options = options;

        var stream = new NetworkStream(acceptedSocket, IO.FileAccess.ReadWrite, true);

        try
        {
            sslStream = new SslStream(stream, false);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public override ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        return sslStream.ReadAsync(buffer, cancellationToken);
    }

    public override ValueTask SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        return sslStream.WriteAsync(buffer, cancellationToken);
    }

    protected override Task StoppingAsync()
    {
        sslStream.Close();
        return Task.CompletedTask;
    }

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        return sslStream.AuthenticateAsServerAsync(options, cancellationToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await using(sslStream.ConfigureAwait(false))
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }


    public override string ToString()
    {
        return $"{Id}-{nameof(SslStreamServerConnection)}-{socket.RemoteEndPoint}";
    }
}