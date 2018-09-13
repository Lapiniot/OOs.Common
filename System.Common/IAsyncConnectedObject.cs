using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public interface IAsyncConnectedObject<TOptions> : IDisposable
    {
        Task ConnectAsync(TOptions options, CancellationToken cancellationToken = default);
        Task CloseAsync();
        bool Connected { get; }
    }
}