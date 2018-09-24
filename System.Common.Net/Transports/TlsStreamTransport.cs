using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Transports
{
    public class TlsStreamTransport : NetworkTransport
    {
        public override Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task OnDisconnectAsync()
        {
            throw new NotImplementedException();
        }

        protected override Task OnConnectAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task OnConnectedAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}