namespace OOs.Threading;

public interface IAsyncCancelable : IAsyncDisposable
{
    bool IsCompleted { get; }
    bool IsCanceled { get; }
    Exception Exception { get; }
}