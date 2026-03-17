namespace OOs.Common.Threading.Tests.AsyncSemaphore;

[TestClass]
public class ReleaseShould
{
    [TestMethod]
    public void ThrowSemaphoreFullException_GivenReleaseCountResultingCurrentCountGreaterThanMaxCount()
    {
        // Arrange
        var semaphore = new OOs.Threading.AsyncSemaphore(0, 1);
        semaphore.WaitAsync();

        // Act/Assert
        Assert.ThrowsExactly<SemaphoreFullException>(() => semaphore.Release(3));

        // Proper cleanup
        semaphore.Release();
    }
}