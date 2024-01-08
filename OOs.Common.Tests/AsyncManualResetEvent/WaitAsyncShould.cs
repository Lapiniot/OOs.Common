namespace OOs.Common.Tests.AsyncManualResetEvent;

[TestClass]
public class WaitAsyncShould
{
    [TestMethod]
    public void ReturnPendingTask_WhenNonSignaled()
    {
        var mre = new Threading.AsyncManualResetEvent(false);

        var actual = mre.WaitAsync(default);

        Assert.IsNotNull(actual);
        Assert.IsFalse(actual.IsCompleted);
    }

    [TestMethod]
    public async Task ReturnCompletedTask_WhenSignaled()
    {
        var mre = new Threading.AsyncManualResetEvent(true);

        var actual = mre.WaitAsync(default);

        Assert.IsNotNull(actual);
        await actual.ConfigureAwait(false);
        Assert.IsTrue(actual.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task ReturnPendingTaskThanCompletes_WhenSetSignaled()
    {
        var mre = new Threading.AsyncManualResetEvent(false);

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
        var mre = new Threading.AsyncManualResetEvent(false);
        using var cts = new CancellationTokenSource();

        var actual = mre.WaitAsync(cts.Token);

        Assert.IsNotNull(actual);
        Assert.IsFalse(actual.IsCompleted);

        cts.Cancel();

        Assert.ThrowsExceptionAsync<OperationCanceledException>(() => mre.WaitAsync(cts.Token));
    }

    [TestMethod]
    public void ReturnCancelledTask_WhenCancelledTokenProvided()
    {
        var mre = new Threading.AsyncManualResetEvent(false);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var task = mre.WaitAsync(cts.Token);

        Assert.IsTrue(task.IsCanceled);
    }
}