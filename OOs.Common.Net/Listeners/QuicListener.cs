using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using OOs.Net.Connections;

#nullable enable

namespace OOs.Net.Listeners;

#if !NET9_0_OR_GREATER
[RequiresPreviewFeatures]
#endif
public sealed class QuicListener : IAsyncEnumerable<TransportConnection>, IDisposable
{
    private readonly IPEndPoint endPoint;
    private readonly SslApplicationProtocol protocol;
    private readonly X509Certificate2 serverCertificate;
    private readonly int backlog;

    public QuicListener(IPEndPoint endPoint, SslApplicationProtocol protocol,
        X509Certificate2 serverCertificate, int backlog = 100)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        ArgumentNullException.ThrowIfNull(serverCertificate);

        this.endPoint = endPoint;
        this.protocol = protocol;
        this.serverCertificate = serverCertificate;
        this.backlog = backlog;
    }

    public override string ToString() => $"{nameof(QuicListener)} (quic://{endPoint})";

    /// <summary>
    /// Gets a value that indicates whether QUIC is supported for server scenarios on the current machine.
    /// </summary>
    /// <returns><see langword="true" /> if QUIC is supported on the current machine and can be used; 
    /// otherwise, <see langword="false" />.
    /// </returns>
    [SupportedOSPlatformGuard("windows")]
    [SupportedOSPlatformGuard("linux")]
    [SupportedOSPlatformGuard("osx")]
#pragma warning disable CA1416 // Validate platform compatibility
    public static bool IsSupported => System.Net.Quic.QuicListener.IsSupported;
#pragma warning restore CA1416 // Validate platform compatibility

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("osx")]
    public async IAsyncEnumerator<TransportConnection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        if (!System.Net.Quic.QuicListener.IsSupported)
        {
            Connections.ThrowHelper.ThrowQuicNotSupported();
            yield break;
        }

        QuicServerConnectionOptions connectionOptions = new()
        {
            IdleTimeout = Timeout.InfiniteTimeSpan,
            // Used to abort stream if it's not properly closed by the user.
            // See https://www.rfc-editor.org/rfc/rfc9000#section-20.2
            DefaultStreamErrorCode = 0x0A, // Protocol-dependent error code.
            // Used to close the connection if it's not done by the user.
            // See https://www.rfc-editor.org/rfc/rfc9000#section-20.2
            DefaultCloseErrorCode = 0x0B, // Protocol-dependent error code.
            ServerAuthenticationOptions = new()
            {
                ApplicationProtocols = [protocol],
                ServerCertificate = serverCertificate
            }
        };

        QuicListenerOptions options = new()
        {
            ListenEndPoint = endPoint,
            ListenBacklog = backlog,
            ApplicationProtocols = [protocol],
            ConnectionOptionsCallback = (_, _, _) => ValueTask.FromResult(connectionOptions)
        };

        var listener = await System.Net.Quic.QuicListener.ListenAsync(options, cancellationToken).ConfigureAwait(false);

        await using (listener.ConfigureAwait(false))
        {
            while (true)
            {
                QuicConnection? acceptedConnection = null;
                ServerQuicTransportConnection? transportConnection = null;

                try
                {
                    acceptedConnection = await listener.AcceptConnectionAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable CA2000 // Dispose objects before losing scope
                    transportConnection = new(acceptedConnection);
#pragma warning restore CA2000 // Dispose objects before losing scope
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    if (acceptedConnection is not null)
                    {
                        await acceptedConnection.DisposeAsync().ConfigureAwait(false);
                    }

                    continue;
                }

                yield return transportConnection;
            }
        }
    }

#pragma warning restore CA1416 // Validate platform compatibility

    public void Dispose() => serverCertificate.Dispose();
}