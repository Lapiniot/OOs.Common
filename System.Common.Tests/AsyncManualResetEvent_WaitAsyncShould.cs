namespace System.Common.Tests;

[TestClass]
public class AsyncManualResetEvent_WaitAsyncShould
{
    [TestMethod]
    public void ReturnPendingTask_WhenNonSignaled()
    {
        var mre = new AsyncManualResetEvent(false);

        var actual = mre.WaitAsync(default);

        Assert.IsNotNull(actual);
        Assert.IsFalse(actual.IsCompleted);
    }

    [TestMethod]
    public async Task ReturnCompletedTask_WhenSignaled()
    {
        var mre = new AsyncManualResetEvent(true);

        var actual = mre.WaitAsync(default);

        Assert.IsNotNull(actual);
        await actual.ConfigureAwait(false);
        Assert.IsTrue(actual.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task ReturnPendingTaskThanCompletes_WhenSetSignaled()
    {
        var mre = new AsyncManualResetEvent(false);

        var actual = mre.WaitAsync(default);

        Assert.IsNotNull(actual);
        Assert.IsFalse(actual.IsCompleted);

        mre.Set();
        await actual.ConfigureAwait(false);

        Assert.IsTrue(actual.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void ReturnPendingTask_ThanThrowsOperationCancelledException_WhenTokenCanceled()
    {
        var mre = new AsyncManualResetEvent(false);
        using var cts = new CancellationTokenSource();

        var actual = mre.WaitAsync(cts.Token);

        Assert.IsNotNull(actual);
        Assert.IsFalse(actual.IsCompleted);

        cts.Cancel();

        Assert.ThrowsExceptionAsync<OperationCanceledException>(() => mre.WaitAsync(cts.Token));
    }

    [TestMethod]
    public void ThrowOperationCancelledExceptionImmidiatelly_WhenCancelledTokenProvided()
    {
        var mre = new AsyncManualResetEvent(false);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsException<OperationCanceledException>(() => mre.WaitAsync(cts.Token));
    }
}