namespace System.Common.Tests.AsyncSemaphore;

[TestClass]
public class ReleaseShould
{
    [TestMethod]
    public void ThrowSemaphoreFullException_GivenReleaseCountResultingCurrentCountGreaterThanMaxCount()
    {
        // Arrange
        var semaphore = new Threading.AsyncSemaphore(0, 1);
        semaphore.WaitAsync();

        // Act/Assert
        Assert.ThrowsException<SemaphoreFullException>(() => semaphore.Release(3));

        // Proper cleanup
        semaphore.Release();
    }
}