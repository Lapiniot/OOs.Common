using System.Threading;
using System.Threading.Tasks;

namespace System.Net
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

        protected override Task OnCloseAsync()
        {
            throw new NotImplementedException();
        }

        protected override Task OnConnectAsync(object options, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task OnConnectedAsync(object options, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}