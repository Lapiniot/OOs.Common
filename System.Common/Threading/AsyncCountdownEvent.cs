#nullable enable
namespace System.Threading;

public class AsyncCountdownEvent
{
    private int signals;

    public AsyncCountdownEvent(int signalCount)
    {
        Verify.ThrowIfLessThan(signalCount, 0);
        signals = signalCount;
    }

    public void AddCount() => AddCount(1);

    public void AddCount(int signalCount) => Interlocked.Add(ref signals, signalCount);

    public void Signal() => Interlocked.Decrement(ref signals);

    public void Signal(int signalCount) => Interlocked.Add(ref signals, -signalCount);

    public Task WaitAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public void Reset() => throw new NotImplementedException();

    public void Reset(int signalCount) => throw new NotImplementedException();
}