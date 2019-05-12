using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public interface INetworkTransport : IAsyncConnectedObject, IDisposable, IAsyncDisposable
    {
        ValueTask<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken);
        ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    }
}