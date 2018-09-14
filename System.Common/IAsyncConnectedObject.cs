using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public interface IAsyncConnectedObject<in TOptions> : IDisposable
    {
        bool Connected { get; }
        Task ConnectAsync(TOptions options, CancellationToken cancellationToken = default);
        Task CloseAsync();
    }
}