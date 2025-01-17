using ASL = OOs.Threading.AsyncSemaphoreLight;

namespace OOs.Common.Tests.AsyncSemaphoreLight;

[TestClass]
public class ReleaseShould
{
    [TestMethod]
    public void ThrowSemaphoreFullException_WhenCurrentCountEqualsToMaxCount()
    {
        var semaphore = new ASL(1, 1);
        Assert.ThrowsException<SemaphoreFullException>(() => semaphore.Release());
    }

    [TestMethod]
    public void DoNotThrowSemaphoreFullException_WhenCurrentCountIsBellowMaxCount()
    {
        var semaphore = new ASL(0, 1);
        semaphore.Release();
    }
}
