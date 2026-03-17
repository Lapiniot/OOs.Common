namespace OOs.Common.Threading.Tests.AsyncSemaphore;

[TestClass]
public class TryResetShould
{
    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenNegativeMaxCount()
    {
        var semaphore = new OOs.Threading.AsyncSemaphore(1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => semaphore.TryReset(0, -1));
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenZeroMaxCount()
    {
        var semaphore = new OOs.Threading.AsyncSemaphore(1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => semaphore.TryReset(0, 0));
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenNegativeInitialCount()
    {
        var semaphore = new OOs.Threading.AsyncSemaphore(1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => semaphore.TryReset(-1));
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenInitialCountGreaterThanMaxCount()
    {
        var semaphore = new OOs.Threading.AsyncSemaphore(1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => semaphore.TryReset(2, 1));
    }

    [TestMethod]
    public void ReturnTrue_SetCurrentCountAndMaxCount_WhenThereAreNoPendingWaitingTasks()
    {
        // Arrange
        var semaphore = new OOs.Threading.AsyncSemaphore(1, 5);

        // Act
        var actual = semaphore.TryReset(3, 10);

        // Assert
        Assert.IsTrue(actual);
        Assert.AreEqual(3, semaphore.CurrentCount);
        Assert.AreEqual(10, semaphore.MaxCount);
    }

    [TestMethod]
    public void ReturnFalse_WhenThereArePendingWaitingTasks()
    {
        // Arrange
        var semaphore = new OOs.Threading.AsyncSemaphore(0);
        _ = semaphore.WaitAsync(); // This will queue a waiter

        // Act
        var actual = semaphore.TryReset(1);

        // Assert
        Assert.IsFalse(actual);
        Assert.AreEqual(0, semaphore.CurrentCount); // Should remain unchanged
    }
}