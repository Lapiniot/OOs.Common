using ASL = OOs.Threading.AsyncSemaphoreLight;

namespace OOs.Common.Threading.Tests.AsyncSemaphoreLight;

[TestClass]
public class AsyncSemaphoreLightReleaseShould
{
    [TestMethod]
    public void ThrowSemaphoreFullException_WhenCurrentCountEqualsToMaxCount()
    {
        var semaphore = new ASL(1, 1);
        Assert.ThrowsExactly<SemaphoreFullException>(() => semaphore.Release());
    }

    [TestMethod]
    public void DoNotThrowSemaphoreFullException_WhenCurrentCountIsBellowMaxCount()
    {
        var semaphore = new ASL(0, 1);
        semaphore.Release();
    }
}