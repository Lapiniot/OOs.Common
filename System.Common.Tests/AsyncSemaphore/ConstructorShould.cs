namespace System.Common.Tests.AsyncSemaphore;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentOutOfRange_GivenNegativeInitialCount() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Threading.AsyncSemaphore(-1));

    [TestMethod]
    public void ThrowArgumentOutOfRange_GivenNegativeMaxCount() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Threading.AsyncSemaphore(0, -1));

    [TestMethod]
    public void ThrowArgumentOutOfRange_GivenZeroMaxCount() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Threading.AsyncSemaphore(0, 0));

    [TestMethod]
    public void ThrowArgumentOutOfRange_GivenInitialCountGraterThanMaxCount() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Threading.AsyncSemaphore(2, 1));

    [TestMethod]
    public void SetCurrentCountProperty_GivenValidInitialCountArg()
    {
        var semaphore = new Threading.AsyncSemaphore(1);

        Assert.AreEqual(1, semaphore.CurrentCount);
    }

    [TestMethod]
    public void SetMaxCountProperty_GivenValidMaxCountArg()
    {
        var semaphore = new Threading.AsyncSemaphore(0, 1);

        Assert.AreEqual(1, semaphore.MaxCount);
    }
}