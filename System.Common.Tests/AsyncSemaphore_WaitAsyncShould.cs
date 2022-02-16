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
        var task = semaphore.WaitAsync(cts.Token);

        // Assert 1
        Assert.IsNotNull(task);
        Assert.IsFalse(task.IsCanceled);
        Assert.AreEqual(TaskStatus.WaitingForActivation, task.Status);
        Assert.AreEqual(0, semaphore.CurrentCount);

        // Act 2
        cts.Cancel();
        semaphore.Release();

        // Assert 2
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => task).ConfigureAwait(false);
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
        Assert.IsFalse(task.IsCanceled);
        Assert.AreEqual(TaskStatus.WaitingForActivation, task.Status);
        Assert.AreEqual(0, semaphore.CurrentCount);

        // Act 2
        semaphore.Release();
        await task.ConfigureAwait(false); // Need to await here, because backing TaskCompletionSource is configured to run continuations asynchronously
        cts.Cancel();

        // Assert 2
        Assert.IsTrue(task.IsCompletedSuccessfully);
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
        Assert.IsFalse(task1.IsCompletedSuccessfully);
        Assert.AreEqual(TaskStatus.WaitingForActivation, task1.Status);
        Assert.AreEqual(0, semaphore.CurrentCount);

        Assert.IsNotNull(task2);
        Assert.IsFalse(task2.IsCompletedSuccessfully);
        Assert.AreEqual(TaskStatus.WaitingForActivation, task2.Status);
        Assert.AreEqual(0, semaphore.CurrentCount);
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
            result.Add(semaphore.WaitAsync());
            return result;
        }, result =>
        {
            lock(tasks)
            {
                tasks.AddRange(result);
            }
        });
        var actualCompleteTasks = tasks.Count(task => task.IsCompletedSuccessfully);
        var actualPendingTasks = tasks.Count(task => !task.IsCompletedSuccessfully && task.Status == TaskStatus.WaitingForActivation);

        // Assert
        Assert.AreEqual(expectedCurrentCount, semaphore.CurrentCount);
        Assert.AreEqual(expectedCompleteTasks, actualCompleteTasks);
        Assert.AreEqual(expectedPendingTasks, actualPendingTasks);
    }
}