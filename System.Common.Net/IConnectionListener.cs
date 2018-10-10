using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public interface IConnectionListener : IDisposable
    {
        bool IsListening { get; }
        void Start();
        void Stop();
        Task<INetworkTransport> AcceptAsync(CancellationToken cancellationToken);
    }
}