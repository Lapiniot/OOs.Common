using ASL = System.Threading.AsyncSemaphoreLight;

namespace System.Common.Tests.AsyncSemaphoreLight;

[TestClass]
public class ReleaseShould
{
    [TestMethod]
    [ExpectedException(typeof(SemaphoreFullException))]
    public void ThrowSemaphoreFullException_WhenCurrentCountEqualsToMaxCount()
    {
        var semaphore = new ASL(1, 1);
        semaphore.Release();
    }

    [TestMethod]
    public void DoNotThrowSemaphoreFullException_WhenCurrentCountIsBellowMaxCount()
    {
        var semaphore = new ASL(0, 1);
        semaphore.Release();
    }
}
