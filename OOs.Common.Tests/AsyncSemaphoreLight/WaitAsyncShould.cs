using ASL = OOs.Threading.AsyncSemaphoreLight;

namespace OOs.Common.Tests.AsyncSemaphoreLight;

[TestClass]
public class WaitAsyncShould
{
    [TestMethod]
    public void DecrementCurrentCount_ReturnCompletedValueTask_WhenCurrentCountIsGraterThanZero()
    {
        var semaphore = new ASL(1, 2);
        var task = semaphore.WaitAsync(default).AsTask();

        Assert.IsTrue(task.IsCompletedSuccessfully);
        Assert.AreEqual(0, semaphore.CurrentCount);
    }

    [TestMethod]
    public void DecrementCurrentCount_ReturnPendingValueTask_WhenCurrentCountEqualsZero()
    {
        var semaphore = new ASL(0, 1);
        var task = semaphore.WaitAsync(default).AsTask();

        Assert.IsFalse(task.IsCompleted);
        Assert.AreEqual(0, semaphore.CurrentCount);
    }

    [TestMethod]
    public void ThrowInvalidOperationException_ForWaitingSemaphore()
    {
        // Arrange
        var semaphore = new ASL(0, 1);
        semaphore.WaitAsync(default).AsTask();
        Assert.ThrowsException<InvalidOperationException>(() =>
                // Act
                semaphore.WaitAsync(default).AsTask());
    }

    [TestMethod]
    public void DoNotAffectCurrentCount_ReturnCancelledValueTask_GivenCancelledToken()
    {
        var semaphore = new ASL(1, 2);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var task = semaphore.WaitAsync(cts.Token).AsTask();

        Assert.IsTrue(task.IsCanceled);
        Assert.AreEqual(1, semaphore.CurrentCount);
    }

    [TestMethod]
    public void ReturnPendingTask_TransitingToCancelled_WhenCancellationRequested()
    {
        // Arrange
        var semaphore = new ASL(0, 1);
        using var cts = new CancellationTokenSource();
        var vt = semaphore.WaitAsync(cts.Token);

        Assert.IsFalse(vt.IsCompleted);
        cts.Cancel();

        Assert.IsTrue(vt.IsCanceled);
        Assert.AreEqual(0, semaphore.CurrentCount);
    }
}
