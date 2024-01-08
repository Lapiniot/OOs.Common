namespace OOs.Common.Tests.AsyncCountdownEvent;

[TestClass]
public class SignalShould
{
    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenNegativeValue() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Threading.AsyncCountdownEvent(1).Signal(-1));

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenZeroValue() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Threading.AsyncCountdownEvent(1).Signal(0));

    [TestMethod]
    public void ThrowInvalidOperationException_GivenValueGreaterThanCurrentCount() =>
        Assert.ThrowsException<InvalidOperationException>(() => new Threading.AsyncCountdownEvent(1).Signal(2));

    [TestMethod]
    public void ThrowInvalidOperationException_WhenEventAlreadySet() =>
        Assert.ThrowsException<InvalidOperationException>(() => new Threading.AsyncCountdownEvent(0).Signal(1));

    [TestMethod]
    public void DecrementCountButDoNotSetEvent_GivenSignalsLessThenCurrentCount()
    {
        var cde = new Threading.AsyncCountdownEvent(2);

        cde.Signal(1);

        Assert.AreEqual(1, cde.CurrentCount);
        Assert.IsFalse(cde.WaitAsync(default).IsCompleted);
    }

    [TestMethod]
    public void DecrementCountAndSetEvent_GivenSignalsEqualToCurrentCount()
    {
        var cde = new Threading.AsyncCountdownEvent(2);

        cde.Signal(2);

        Assert.AreEqual(0, cde.CurrentCount);
        Assert.IsTrue(cde.WaitAsync(default).IsCompletedSuccessfully);
    }
}