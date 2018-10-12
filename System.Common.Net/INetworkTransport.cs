using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public interface INetworkTransport : IAsyncConnectedObject
    {
        Task<int> SendAsync(Memory<byte> buffer, CancellationToken cancellationToken);
        Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    }
}