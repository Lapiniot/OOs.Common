namespace System.Common.Tests;

[TestClass]
public class AsyncCountdownEvent_AddCountShould
{
    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenNegativeValue() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new AsyncCountdownEvent(1).AddCount(-1));

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenZeroValue() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new AsyncCountdownEvent(1).AddCount(0));

    [TestMethod]
    public void ThrowInvalidOperationException_WhenEventAlreadySet() =>
        Assert.ThrowsException<InvalidOperationException>(() => new AsyncCountdownEvent(0).AddCount(1));

    [TestMethod]
    public void ThrowInvalidOperationException_GivenSignalsMoreThanMaxPossible() =>
        Assert.ThrowsException<InvalidOperationException>(() => new AsyncCountdownEvent(1).AddCount(int.MaxValue));

    [TestMethod]
    public void IncrementCurrentCountButDoNotSetEvent()
    {
        var cde = new AsyncCountdownEvent(1);

        cde.AddCount(2);

        Assert.AreEqual(3, cde.CurrentCount);
        Assert.IsFalse(cde.WaitAsync().IsCompleted);
    }
}