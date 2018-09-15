using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public interface IAsyncConnectedObject : IDisposable
    {
        bool Connected { get; }
        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task CloseAsync();
    }
}