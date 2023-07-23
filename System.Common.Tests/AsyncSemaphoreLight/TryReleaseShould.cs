using ASL = System.Threading.AsyncSemaphoreLight;

namespace System.Common.Tests.AsyncSemaphoreLight;

[TestClass]
public class TryReleaseShould
{

    [TestMethod]
    public void ReturnTrue_IncrementCurrentCount_WhenCurrentCountIsGreaterThanZero()
    {
        var semaphore = new ASL(1, 2);

        var actual = semaphore.TryRelease();

        Assert.IsTrue(actual);
        Assert.AreEqual(2, semaphore.CurrentCount);
    }

    [TestMethod]
    public void ReturnTrue_IncrementCurrentCount_WhenCurrentCountIsZeroAndNoPendingWaiter()
    {
        var semaphore = new ASL(0, 2);

        var actual = semaphore.TryRelease();

        Assert.IsTrue(actual);
        Assert.AreEqual(1, semaphore.CurrentCount);
    }

    [TestMethod]
    public void ReturnTrue_NotIncrementCurrentCount_UnblockWaiter_WhenCurrentCountIsZeroAndWaiting()
    {
        var semaphore = new ASL(0, 2);
        var vt = semaphore.WaitAsync(default);

        Assert.IsFalse(vt.IsCompleted);

        var actual = semaphore.TryRelease();

        Assert.IsTrue(actual);
        Assert.AreEqual(0, semaphore.CurrentCount);
        Assert.IsTrue(vt.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void ReturnFalse_NotIncrementCurrentCount_WhenCurrentCountIsEqualToMaxCount()
    {
        var semaphore = new ASL(2, 2);

        var actual = semaphore.TryRelease();

        Assert.IsFalse(actual);
        Assert.AreEqual(2, semaphore.CurrentCount);
    }

    [TestMethod]
    public void NotThrow_NotChangeWaiterValueTaskState_WhenCalledAfterCancellationRequested()
    {
        using var cts = new CancellationTokenSource();
        var semaphore = new ASL(0, 1);
        var vt = semaphore.WaitAsync(cts.Token);

        Assert.IsFalse(vt.IsCompleted);

        cts.Cancel();
        semaphore.TryRelease();

        Assert.IsTrue(vt.IsCanceled);
        Assert.AreEqual(1, semaphore.CurrentCount);
    }

    [TestMethod]
    public void DiscardCancellationTokenRegistrationState()
    {
        using var cts = new CancellationTokenSource();
        var semaphore = new ASL(0, 1);
        var vt = semaphore.WaitAsync(cts.Token);
        semaphore.TryRelease();

        vt = semaphore.WaitAsync(default);
        cts.Cancel();

        Assert.IsFalse(vt.IsCompleted);
    }
}