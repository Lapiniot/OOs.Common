using System.Net.Connections;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Listeners;

public sealed class TcpSslSocketListener : TcpSocketListenerBase, IDisposable
{
    private readonly SslServerAuthenticationOptions options;
    private readonly X509Certificate serverCertificate;
    private bool disposed;

    public TcpSslSocketListener(IPEndPoint endPoint, int backlog = 100,
        Action<Socket> configureListening = null, Action<Socket> configureAccepted = null,
        X509Certificate serverCertificate = null, SslProtocols enabledSslProtocols = SslProtocols.None,
        RemoteCertificateValidationCallback remoteCertificateValidationCallback = null,
        ServerCertificateSelectionCallback serverCertificateSelectionCallback = null,
        bool clientCertificateRequired = false) :
        base(endPoint, backlog, configureListening, configureAccepted)
    {
        this.serverCertificate = serverCertificate;

        options = new()
        {
            ServerCertificate = serverCertificate,
            EnabledSslProtocols = enabledSslProtocols,
            RemoteCertificateValidationCallback = remoteCertificateValidationCallback,
            ServerCertificateSelectionCallback = serverCertificateSelectionCallback,
            ClientCertificateRequired = clientCertificateRequired
        };
    }

    public override string ToString() => $"{nameof(TcpSslSocketListener)} (tcps://{EndPoint})";

    protected override NetworkConnection CreateConnection(Socket acceptedSocket) =>
        new TcpSslServerSocketConnection(acceptedSocket, options);

    public void Dispose()
    {
        if (disposed) return;
        serverCertificate.Dispose();
        disposed = true;
    }
}