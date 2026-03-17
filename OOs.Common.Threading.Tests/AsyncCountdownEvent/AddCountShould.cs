namespace OOs.Common.Threading.Tests.AsyncCountdownEvent;

[TestClass]
public class AddCountShould
{
    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenNegativeValue() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new OOs.Threading.AsyncCountdownEvent(1).AddCount(-1));

    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenZeroValue() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new OOs.Threading.AsyncCountdownEvent(1).AddCount(0));

    [TestMethod]
    public void ThrowInvalidOperationException_WhenEventAlreadySet() =>
        Assert.ThrowsExactly<InvalidOperationException>(() => new OOs.Threading.AsyncCountdownEvent(0).AddCount(1));

    [TestMethod]
    public void ThrowInvalidOperationException_GivenSignalsMoreThanMaxPossible() =>
        Assert.ThrowsExactly<InvalidOperationException>(() => new OOs.Threading.AsyncCountdownEvent(1).AddCount(int.MaxValue));

    [TestMethod]
    public void IncrementCurrentCountButDoNotSetEvent()
    {
        var cde = new OOs.Threading.AsyncCountdownEvent(1);

        cde.AddCount(2);

        Assert.AreEqual(3, cde.CurrentCount);
        Assert.IsFalse(cde.WaitAsync(default).IsCompleted);
    }
}