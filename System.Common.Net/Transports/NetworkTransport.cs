using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Transports
{
    public abstract class NetworkTransport : ConnectedObject, INetworkTransport
    {
        public abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

        public abstract ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    }
}