using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public interface IAsyncConnectedObject : IDisposable
    {
        bool IsConnected { get; }
        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task DisconnectAsync();
    }
}