using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.Connections.Exceptions;
using System.Net.Security;

using static System.Net.Sockets.SocketError;

namespace System.Net.Connections
{
    public sealed class SslStreamClientConnection : TcpSocketClientConnection
    {
        private readonly string machineName;
        private SslStream sslStream;
        private NetworkStream networkStream;

        public SslStreamClientConnection(IPEndPoint endPoint, string machineName) :
            base(endPoint)
        {
            this.machineName = machineName ?? throw new ArgumentNullException(nameof(machineName));
        }

        public SslStreamClientConnection(string hostNameOrAddress, int port, string machineName = null) :
            base(hostNameOrAddress, port)
        {
            this.machineName = machineName ?? hostNameOrAddress;
        }

        public override async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            CheckState(true);

            try
            {
                var vt = sslStream.ReadAsync(buffer, cancellationToken);
                return vt.IsCompletedSuccessfully ? vt.Result : await vt.ConfigureAwait(false);
            }
            catch(SocketException se) when(
                se.SocketErrorCode == ConnectionAborted ||
                se.SocketErrorCode == ConnectionReset)
            {
                await StopActivityAsync().ConfigureAwait(false);
                throw new ConnectionAbortedException(se);
            }
        }

        public override async ValueTask SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            CheckState(true);

            try
            {
                var vt = sslStream.WriteAsync(buffer, cancellationToken);
                if(!vt.IsCompletedSuccessfully)
                {
                    await vt.ConfigureAwait(false);
                }
            }
            catch(SocketException se) when(
                se.SocketErrorCode == ConnectionAborted ||
                se.SocketErrorCode == ConnectionReset)
            {
                await StopActivityAsync().ConfigureAwait(false);
                throw new ConnectionAbortedException(se);
            }
        }

        protected override async Task StartingAsync(object state, CancellationToken cancellationToken)
        {
            await base.StartingAsync(state, cancellationToken).ConfigureAwait(false);

            networkStream = new NetworkStream(Socket, IO.FileAccess.ReadWrite, false);

            try
            {
                sslStream = new SslStream(networkStream, true);

                try
                {
                    var options = new SslClientAuthenticationOptions()
                    {
                        TargetHost = machineName
                    };

                    await sslStream.AuthenticateAsClientAsync(options, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    await using(sslStream.ConfigureAwait(false))
                    {
                        throw;
                    }
                }
            }
            catch
            {
                await using(networkStream.ConfigureAwait(false))
                {
                    throw;
                }
            }
        }

        public override async ValueTask DisposeAsync()
        {
            try
            {
                if(sslStream is not null)
                {
                    await sslStream.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                try
                {
                    if(networkStream is not null)
                    {
                        await networkStream.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    await base.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}