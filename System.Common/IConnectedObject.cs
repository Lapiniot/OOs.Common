using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public interface IConnectedObject
    {
        bool IsConnected { get; }
        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task DisconnectAsync();
    }
}