namespace OOs.Common.Threading.Tests.AsyncCountdownEvent;

[TestClass]
public class AddCountOneShould
{
    [TestMethod]
    public void ThrowInvalidOperationException_WhenEventAlreadySet() =>
        Assert.ThrowsExactly<InvalidOperationException>(() => new OOs.Threading.AsyncCountdownEvent(0).AddCount());

    [TestMethod]
    public void ThrowInvalidOperationException_GivenSignalsMoreThanMaxPossible() =>
        Assert.ThrowsExactly<InvalidOperationException>(() => new OOs.Threading.AsyncCountdownEvent(int.MaxValue).AddCount());

    [TestMethod]
    public void IncrementCurrentCountButDoNotSetEvent()
    {
        var cde = new OOs.Threading.AsyncCountdownEvent(1);

        cde.AddCount();

        Assert.AreEqual(2, cde.CurrentCount);
        Assert.IsFalse(cde.WaitAsync(default).IsCompleted);
    }
}