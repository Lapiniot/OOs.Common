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
    public void RetainCurrentCount_GivenZeroReleaseCount()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(1);
        // Act
        semaphore.Release(0);
        // Assert
        Assert.AreEqual(1, semaphore.CurrentCount);
    }

    [TestMethod]
    public async Task RetainCurrentCountButCompleteTasks_WhenThereArePendingWaitingTasks()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(0);
        var task1 = semaphore.WaitAsync().AsTask();
        var task2 = semaphore.WaitAsync().AsTask();

        // Act
        semaphore.Release(2);
        await Task.WhenAll(task1, task2).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(0, semaphore.CurrentCount);
    }

    [TestMethod]
    public void DoNotCompleteWaitingTask_GivenZeroReleaseCount()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(0);
        var task = semaphore.WaitAsync().AsTask();

        // Act
        semaphore.Release(0);

        // Assert
        Assert.AreEqual(TaskStatus.WaitingForActivation, task.Status);
    }

    [TestMethod]
    public void ThrowSemaphoreFullException_GivenReleaseCountResultingCurrentCountGreaterThanMaxCount()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(0, 1);
        _ = semaphore.WaitAsync();

        // Act/Assert
        Assert.ThrowsException<SemaphoreFullException>(() => semaphore.Release(3));
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
        for(var i = 0; i < waitersCount; i++) waiters.Add(semaphore.WaitAsync().AsTask());

        // Act
        Parallel.For(0, iterations, _ => semaphore.Release());

        // Assert
        Assert.AreEqual(expectedCompleteCount, waiters.Count(task => task.IsCompletedSuccessfully));
        Assert.AreEqual(expectedPendingCount, waiters.Count(task => !task.IsCompletedSuccessfully && task.Status == TaskStatus.WaitingForActivation));
        Assert.AreEqual(expectedCurrentCount, semaphore.CurrentCount);
    }
}