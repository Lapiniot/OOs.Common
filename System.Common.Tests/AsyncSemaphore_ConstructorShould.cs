namespace System.Common.Tests;

[TestClass]
public class AsyncSemaphore_ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentOutOfRange_GivenNegativeInitialCount() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => { _ = new AsyncSemaphore(-1); });

    [TestMethod]
    public void ThrowArgumentOutOfRange_GivenNegativeMaxCount() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => { _ = new AsyncSemaphore(0, -1); });

    [TestMethod]
    public void ThrowArgumentOutOfRange_GivenZeroMaxCount() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => { _ = new AsyncSemaphore(0, 0); });

    [TestMethod]
    public void ThrowArgumentOutOfRange_GivenInitialCountGraterThanMaxCount() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => { _ = new AsyncSemaphore(2, 1); });

    [TestMethod]
    public void SetCurrentCountProperty_GivenValidInitialCountArg()
    {
        var semaphore = new AsyncSemaphore(1);

        Assert.AreEqual(1, semaphore.CurrentCount);
    }

    [TestMethod]
    public void SetMaxCountProperty_GivenValidMaxCountArg()
    {
        var semaphore = new AsyncSemaphore(0, 1);

        Assert.AreEqual(1, semaphore.MaxCount);
    }
}