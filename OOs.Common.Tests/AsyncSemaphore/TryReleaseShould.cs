namespace OOs.Common.Tests.AsyncSemaphore;

[TestClass]
public class TryReleaseShould
{
    [TestMethod]
    public void ReturnTrue_IncrementCurrentCount_WhenThereAreNoPendingWaitingTasks()
    {
        // Arrange
        var semaphore = new Threading.AsyncSemaphore(1);

        // Act
        var actual = semaphore.TryRelease(2);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(3, semaphore.CurrentCount);
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenNegativeReleaseCount()
    {
        var semaphore = new Threading.AsyncSemaphore(1);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => semaphore.TryRelease(-1));
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenZeroReleaseCount()
    {
        var semaphore = new Threading.AsyncSemaphore(1);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => semaphore.TryRelease(0));
    }

    [TestMethod]
    public void ReturnTrue_RetainCurrentCountButCompleteTasks_WhenThereArePendingWaitingTasks()
    {
        // Arrange
        var semaphore = new Threading.AsyncSemaphore(0);
        var task1 = semaphore.WaitAsync();
        var task2 = semaphore.WaitAsync();

        // Act
        var actual = semaphore.TryRelease(2);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(0, semaphore.CurrentCount);
        Assert.IsTrue(task1.IsCompletedSuccessfully);
        Assert.IsTrue(task2.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void ReturnFalse_DoNotThrowSemaphoreFullException_GivenReleaseCountResultingCurrentCountGreaterThanMaxCount()
    {
        // Arrange
        var semaphore = new Threading.AsyncSemaphore(0, 1);
        semaphore.WaitAsync();

        // Act
        var actual = semaphore.TryRelease(3);

        // Assert
        Assert.IsFalse(actual);
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
        var semaphore = new Threading.AsyncSemaphore(initialCount);
        var waiters = new List<Task>(waitersCount);
        for (var i = 0; i < waitersCount; i++) waiters.Add(semaphore.WaitAsync());

        // Act
        Parallel.For(0, iterations, _ => semaphore.TryRelease(1));
        var actualCompleteCount = waiters.Count(task => task.IsCompletedSuccessfully);
        var actualPendingCount = waiters.Count(task => !task.IsCompletedSuccessfully);

        // Assert
        Assert.AreEqual(expectedCompleteCount, actualCompleteCount);
        Assert.AreEqual(expectedPendingCount, actualPendingCount);
        Assert.AreEqual(expectedCurrentCount, semaphore.CurrentCount);

        // Proper cleanup
        if (actualPendingCount > 0)
            semaphore.TryRelease(actualPendingCount);
    }
}