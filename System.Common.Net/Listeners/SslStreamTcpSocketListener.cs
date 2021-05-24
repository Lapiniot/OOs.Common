using System.Net.Connections;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Listeners
{
    public sealed class SslStreamTcpSocketListener : TcpSocketListenerBase, IDisposable
    {
        private readonly SslServerAuthenticationOptions options;
        private readonly X509Certificate serverCertificate;
        private bool disposed;

        public SslStreamTcpSocketListener(IPEndPoint endPoint, X509Certificate serverCertificate, int backlog = 100,
            Action<Socket> configureListening = null, Action<Socket> configureAccepted = null) :
            base(endPoint, backlog, configureListening, configureAccepted)
        {
            this.serverCertificate = serverCertificate ?? throw new ArgumentNullException(nameof(serverCertificate));

            options = new SslServerAuthenticationOptions()
            {
                ServerCertificate = serverCertificate
            };
        }

        public override string ToString()
        {
            return $"{nameof(SslStreamTcpSocketListener)} {{tcps://{EndPoint}}}";
        }

        protected override INetworkConnection CreateConnection(Socket acceptedSocket)
        {
            return new SslStreamServerConnection(acceptedSocket, options);
        }

        public void Dispose()
        {
            if(!disposed)
            {
                serverCertificate.Dispose();
                disposed = true;
            }
        }
    }
}