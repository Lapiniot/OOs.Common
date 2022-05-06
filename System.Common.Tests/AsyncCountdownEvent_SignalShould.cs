namespace System.Common.Tests;

[TestClass]
public class AsyncCountdownEvent_SignalShould
{
    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenNegativeValue() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new AsyncCountdownEvent(1).Signal(-1));

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenZeroValue() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new AsyncCountdownEvent(1).Signal(0));

    [TestMethod]
    public void ThrowInvalidOperationException_GivenValueGreaterThanCurrentCount() =>
        Assert.ThrowsException<InvalidOperationException>(() => new AsyncCountdownEvent(1).Signal(2));

    [TestMethod]
    public void ThrowInvalidOperationException_WhenEventAlreadySet() =>
        Assert.ThrowsException<InvalidOperationException>(() => new AsyncCountdownEvent(0).Signal(1));

    [TestMethod]
    public void DecrementsCountButDoesNotSetEvent_GivenSignalsLessThenCurrentCount()
    {
        var cde = new AsyncCountdownEvent(2);

        cde.Signal(1);

        Assert.AreEqual(1, cde.CurrentCount);
        Assert.IsFalse(cde.WaitAsync().IsCompleted);
    }

    [TestMethod]
    public void DecrementsCountAndSetsEvent_GivenSignalsEqualToCurrentCount()
    {
        var cde = new AsyncCountdownEvent(2);

        cde.Signal(2);

        Assert.AreEqual(0, cde.CurrentCount);
        Assert.IsTrue(cde.WaitAsync().IsCompletedSuccessfully);
    }
}