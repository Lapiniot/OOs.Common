using ASL = System.Threading.AsyncSemaphoreLight;

namespace System.Common.Tests.AsyncSemaphoreLight;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentOutOfRange_GivenNegativeInitialCount() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ASL(-1));

    [TestMethod]
    public void ThrowArgumentOutOfRange_GivenNegativeMaxCount() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ASL(0, -1));

    [TestMethod]
    public void ThrowArgumentOutOfRange_GivenZeroMaxCount() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ASL(0, 0));

    [TestMethod]
    public void ThrowArgumentOutOfRange_GivenInitialCountGraterThanMaxCount() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ASL(2, 1));

    [TestMethod]
    public void SetCurrentCountProperty_GivenValidInitialCountArg()
    {
        var semaphore = new ASL(1);

        Assert.AreEqual(1, semaphore.CurrentCount);
    }

    [TestMethod]
    public void SetMaxCountProperty_GivenValidMaxCountArg()
    {
        var semaphore = new ASL(0, 1);

        Assert.AreEqual(1, semaphore.MaxCount);
    }
}