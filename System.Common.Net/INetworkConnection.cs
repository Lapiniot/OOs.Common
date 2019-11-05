using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public interface INetworkConnection : IConnectedObject, IAsyncDisposable
    {
        ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken);
        ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    }
}