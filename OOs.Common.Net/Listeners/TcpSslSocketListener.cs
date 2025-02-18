using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using OOs.Net.Connections;

namespace OOs.Net.Listeners;

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

    protected override TransportConnection CreateConnection(Socket acceptedSocket) =>
        new ServerTcpSslSocketTransportConnection(acceptedSocket, options);

    public override string ToString() => $"{nameof(TcpSslSocketListener)} (tcps://{EndPoint})";

    public void Dispose()
    {
        if (disposed) return;
        serverCertificate.Dispose();
        disposed = true;
    }
}