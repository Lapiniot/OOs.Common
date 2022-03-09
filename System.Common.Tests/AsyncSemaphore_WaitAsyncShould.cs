namespace System.Common.Tests;

[TestClass]
public class AsyncSemaphore_WaitAsyncShould
{
    [TestMethod]
    public void ReturnCancelledTaskAndRetainCurrentCount_GivenAlreadyCanceledToken()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(1);
        using var cts = new CancellationTokenSource(0);

        // Act
        var task = semaphore.WaitAsync(cts.Token);

        // Assert
        Assert.IsNotNull(task);
        Assert.IsTrue(task.IsCanceled);
        Assert.AreEqual(1, semaphore.CurrentCount);
    }

    [TestMethod]
    public async Task ReturnTaskWhichTransitsToCancelled_WhenCurrentCountZeroAndCancellationRequestedBeforeReleaseCalled()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(0);
        using var cts = new CancellationTokenSource();

        // Act 1
        var task1 = semaphore.WaitAsync(default);
        var task2 = semaphore.WaitAsync(default);
        var task3 = semaphore.WaitAsync(cts.Token);
        var task4 = semaphore.WaitAsync(default);

        // Assert 1
        Assert.IsNotNull(task3);
        Assert.IsFalse(task3.IsCompleted);
        Assert.AreEqual(0, semaphore.CurrentCount);

        // Act 2
        cts.Cancel();
        semaphore.Release();
        semaphore.Release();
        semaphore.Release();

        // Assert 2
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () => await task3.ConfigureAwait(false)).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReturnTaskWhichTransitsToCompleted_WhenCurrentCountZeroAndCancellationRequestedAfterReleaseCalled()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(0);
        using var cts = new CancellationTokenSource();

        // Act 1
        var task = semaphore.WaitAsync(cts.Token);

        // Assert 1
        Assert.IsNotNull(task);
        Assert.IsFalse(task.IsCompleted);
        Assert.AreEqual(0, semaphore.CurrentCount);

        // Act 2
        semaphore.Release();
        cts.Cancel();

        // Assert 2
        await task.ConfigureAwait(false);
    }

    [TestMethod]
    public void ReturnCompletedTaskAndDecrementCurrentCount_WhenCurrentCountIsGreaterThanZero()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(1);

        // Act
        var task = semaphore.WaitAsync();

        // Assert
        Assert.IsNotNull(task);
        Assert.IsTrue(task.IsCompletedSuccessfully);
        Assert.AreEqual(0, semaphore.CurrentCount);
    }

    [TestMethod]
    public void ReturnPendingTaskAndRetainCurrentCount_WhenCurrentCountIsZero()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(0);

        // Act
        var task1 = semaphore.WaitAsync();
        var task2 = semaphore.WaitAsync();

        // Assert
        Assert.IsNotNull(task1);
        Assert.IsFalse(task1.IsCompleted);
        Assert.AreEqual(0, semaphore.CurrentCount);

        Assert.IsNotNull(task2);
        Assert.IsFalse(task2.IsCompleted);
        Assert.AreEqual(0, semaphore.CurrentCount);

        // Proper cleanup
        semaphore.Release(2);
    }

    [TestMethod]
    [DataRow(3, 2, 1, 2, 0, DisplayName = "ReturnsRequestedNumberOfCompleteTasks_GivenRequestsLessThanCurrentCount")]
    [DataRow(2, 4, 0, 2, 2, DisplayName = "ReturnsRequestedNumberOfCompleteTasksAndExtraPending_GivenRequestsMoreThanCurrentCount")]
    public void ReturnTasksAndUpdateCurrentCountThreadSafely_CalledInParallel(int initialCount, int iterations,
        int expectedCurrentCount, int expectedCompleteTasks, int expectedPendingTasks)
    {
        // Arrange
        var semaphore = new AsyncSemaphore(initialCount);
        var tasks = new List<Task>();

        // Act
        Parallel.For(0, iterations, () => new List<Task>(), (_, _, result) =>
        {
#pragma warning disable CA2012 // Use ValueTasks correctly
            result.Add(semaphore.WaitAsync());
#pragma warning restore CA2012 // Use ValueTasks correctly
            return result;
        }, result =>
        {
            lock (tasks)
            {
                tasks.AddRange(result);
            }
        });
        var actualCompleteTasks = tasks.Count(task => task.IsCompletedSuccessfully);
        var actualPendingTasks = tasks.Count(task => !task.IsCompleted);

        // Assert
        Assert.AreEqual(expectedCurrentCount, semaphore.CurrentCount);
        Assert.AreEqual(expectedCompleteTasks, actualCompleteTasks);
        Assert.AreEqual(expectedPendingTasks, actualPendingTasks);

        // Proper cleanup
        semaphore.Release(actualPendingTasks);
    }
}