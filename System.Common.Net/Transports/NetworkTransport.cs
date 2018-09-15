using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Transports
{
    public abstract class NetworkTransport : AsyncConnectedObject<object>, INetworkTransport
    {
        public abstract Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

        public abstract Task<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    }
}