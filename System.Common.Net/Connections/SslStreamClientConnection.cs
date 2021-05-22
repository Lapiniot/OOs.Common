using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.Connections.Exceptions;
using System.Net.Security;

using static System.Net.Dns;
using static System.Net.Sockets.ProtocolType;
using static System.Net.Sockets.SocketType;
using static System.Net.Sockets.SocketError;

namespace System.Net.Connections
{
    public sealed class SslStreamClientConnection : NetworkConnection
    {
        private IPEndPoint remoteEndPoint;
        private readonly string machineName;
        private string hostNameOrAddress;
        private SslStream sslStream;
        private readonly int port;

        public SslStreamClientConnection(IPEndPoint endPoint, string machineName)
        {
            this.remoteEndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            this.machineName = machineName ?? throw new ArgumentNullException(nameof(machineName));
        }

        public SslStreamClientConnection(string hostNameOrAddress, int port, string machineName = null)
        {
            if(string.IsNullOrWhiteSpace(hostNameOrAddress))
            {
                throw new ArgumentException($"'{nameof(hostNameOrAddress)}' cannot be null or whitespace.", nameof(hostNameOrAddress));
            }

            this.hostNameOrAddress = hostNameOrAddress;
            this.port = port;
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
            try
            {
                if(remoteEndPoint == null)
                {
                    var addresses = await GetHostAddressesAsync(hostNameOrAddress, cancellationToken).ConfigureAwait(false);
                    remoteEndPoint = new IPEndPoint(addresses[0], port);
                }

                var socket = new Socket(remoteEndPoint.AddressFamily, Stream, Tcp);

                try
                {
                    await socket.ConnectAsync(remoteEndPoint, cancellationToken).ConfigureAwait(false);

                    var stream = new NetworkStream(socket, IO.FileAccess.ReadWrite, true);

                    try
                    {
                        sslStream = new SslStream(stream, false);

                        var options = new SslClientAuthenticationOptions()
                        {
                            TargetHost = machineName
                        };

                        await sslStream.AuthenticateAsClientAsync(options, cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        await using(stream.ConfigureAwait(false))
                        {
                            throw;
                        }
                    }
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
            catch(SocketException se) when(se.SocketErrorCode == HostNotFound)
            {
                throw new HostNotFoundException(se);
            }
            catch(SocketException se)
            {
                throw new ServerUnavailableException(se);
            }
        }

        protected override Task StoppingAsync()
        {
            sslStream?.Close();
            sslStream = null;
            return Task.CompletedTask;
        }

        public override async ValueTask DisposeAsync()
        {
            try
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                if(sslStream is not null)
                {
                    await sslStream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}