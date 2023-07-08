namespace System.Common.Tests;

[TestClass]
public class AsyncSemaphore_ReleaseShould
{
    [TestMethod]
    public void ThrowSemaphoreFullException_GivenReleaseCountResultingCurrentCountGreaterThanMaxCount()
    {
        // Arrange
        var semaphore = new AsyncSemaphore(0, 1);
        semaphore.WaitAsync();

        // Act/Assert
        Assert.ThrowsException<SemaphoreFullException>(() => semaphore.Release(3));

        // Proper cleanup
        semaphore.Release();
    }
}