using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public interface IAsyncConnectedObject
    {
        bool IsConnected { get; }
        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task DisconnectAsync();
    }
}