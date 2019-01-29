using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public interface IConnectionListener : IDisposable, IAsyncEnumerable<INetworkTransport>
    {
        bool IsListening { get; }
        void Start();
        void Stop();
        Task<INetworkTransport> AcceptAsync(CancellationToken cancellationToken);
    }
}