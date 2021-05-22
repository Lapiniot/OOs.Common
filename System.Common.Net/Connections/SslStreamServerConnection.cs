using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    public sealed class SslStreamServerConnection : NetworkConnection
    {
        private SslStream sslStream;
        private readonly SslServerAuthenticationOptions options;

        public SslStreamServerConnection(SslStream sslStream, SslServerAuthenticationOptions options)
        {
            this.sslStream = sslStream ?? throw new ArgumentNullException(nameof(sslStream));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
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

        protected override Task StartingAsync(object state, CancellationToken cancellationToken)
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
    }
}