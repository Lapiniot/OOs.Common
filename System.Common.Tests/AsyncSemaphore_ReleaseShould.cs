namespace System.Common.Tests;

[TestClass]
public class AsyncSemaphore_ReleaseShould
{
    [TestMethod]
    public void IncrementCurrentCount_WhenThereAreNoPendingWaitingTasks()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(1);
        // Act
        semaphore.Release(2);
        // Assert
        Assert.AreEqual(3, semaphore.CurrentCount);
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenNegativeReleaseCount()
    {
        var semaphore = new AsyncSemaphore(1);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => semaphore.Release(-1));
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenZeroReleaseCount()
    {
        var semaphore = new AsyncSemaphore(1);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => semaphore.Release(0));
    }

    [TestMethod]
    public void RetainCurrentCountButCompleteTasks_WhenThereArePendingWaitingTasks()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(0);
        var task1 = semaphore.WaitAsync();
        var task2 = semaphore.WaitAsync();

        // Act
        semaphore.Release(2);

        // Assert
        Assert.AreEqual(0, semaphore.CurrentCount);
        Assert.IsTrue(task1.IsCompletedSuccessfully);
        Assert.IsTrue(task2.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void ThrowSemaphoreFullException_GivenReleaseCountResultingCurrentCountGreaterThanMaxCount()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(0, 1);
        _ = semaphore.WaitAsync();

        // Act/Assert
        Assert.ThrowsException<SemaphoreFullException>(() => semaphore.Release(3));

        // Proper cleanup
        semaphore.Release();
    }

    [TestMethod]
    [DataRow(0, 3, 10, 3, 0, 7, DisplayName = "CompleteAllWaitersThenIncrementCurrentCount_GivenMoreReleasesThanWaits_ZeroInitialCount")]
    [DataRow(2, 5, 10, 5, 0, 7, DisplayName = "CompleteAllWaitersThenIncrementCurrentCount_GivenMoreReleasesThanWaits_NonZeroInitialCount")]
    [DataRow(0, 10, 5, 5, 5, 0, DisplayName = "CompleteExactlyReleaseCountWaiters_GivenLessReleasesThanWaits_ZeroInitialCount")]
    [DataRow(2, 10, 5, 7, 3, 0, DisplayName = "CompleteExactlyReleaseCountWaiters_GivenLessReleasesThanWaits_NonZeroInitialCount")]
    public void CompletePendingWaitersAndIncrementCurrentCountThreadSafely_CalledInParallel(
        int initialCount, int waitersCount, int iterations,
        int expectedCompleteCount, int expectedPendingCount, int expectedCurrentCount)
    {
        // Arrange
        var semaphore = new AsyncSemaphore(initialCount);
        var waiters = new List<Task>(waitersCount);
        for (var i = 0; i < waitersCount; i++) waiters.Add(semaphore.WaitAsync());

        // Act
        Parallel.For(0, iterations, _ => semaphore.Release());
        var actualCompleteCount = waiters.Count(task => task.IsCompletedSuccessfully);
        var actualPendingCount = waiters.Count(task => !task.IsCompletedSuccessfully);

        // Assert
        Assert.AreEqual(expectedCompleteCount, actualCompleteCount);
        Assert.AreEqual(expectedPendingCount, actualPendingCount);
        Assert.AreEqual(expectedCurrentCount, semaphore.CurrentCount);

        // Proper cleanup
        if (actualPendingCount > 0)
            semaphore.Release(actualPendingCount);
    }
}