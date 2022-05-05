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
}