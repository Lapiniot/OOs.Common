using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    public abstract class NetworkConnection : ConnectedObject, INetworkConnection
    {
        public abstract ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

        public abstract ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    }
}