namespace OOs.Common.Threading.Tests.AsyncCountdownEvent;

[TestClass]
public class WaitAsyncShould
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void ReturnCancelledTaskAndRetainCurrentCount_GivenAlreadyCanceledToken()
    {
        var cde = new OOs.Threading.AsyncCountdownEvent(0);
        using var cts = new CancellationTokenSource(0);

        var task = cde.WaitAsync(cts.Token);

        Assert.IsNotNull(task);
        Assert.IsTrue(task.IsCanceled);
        Assert.AreEqual(0, cde.CurrentCount);
    }

    [TestMethod]
    [Timeout(500, CooperativeCancellation = true)]
    public async Task ReturnTaskThatTransitionsToCanceled_WhenTokenCanceledWhileWaiting()
    {
        var cde = new OOs.Threading.AsyncCountdownEvent(1);
        using var cts = new CancellationTokenSource();

        var task = cde.WaitAsync(cts.Token);

        Assert.IsNotNull(task);
        Assert.IsFalse(task.IsCompleted);

        await cts.CancelAsync().ConfigureAwait(false);
        await await Task.WhenAny(
            task1: Assert.ThrowsAsync<OperationCanceledException>(() => task),
            task2: task.WaitAsync(TestContext.CancellationToken));
    }
}