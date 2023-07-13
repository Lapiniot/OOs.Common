namespace System.Common.Tests.AsyncCountdownEvent;

[TestClass]
public class AddCountOneShould
{
    [TestMethod]
    public void ThrowInvalidOperationException_WhenEventAlreadySet() =>
        Assert.ThrowsException<InvalidOperationException>(() => new Threading.AsyncCountdownEvent(0).AddCount());

    [TestMethod]
    public void ThrowInvalidOperationException_GivenSignalsMoreThanMaxPossible() =>
        Assert.ThrowsException<InvalidOperationException>(() => new Threading.AsyncCountdownEvent(int.MaxValue).AddCount());

    [TestMethod]
    public void IncrementCurrentCountButDoNotSetEvent()
    {
        var cde = new Threading.AsyncCountdownEvent(1);

        cde.AddCount();

        Assert.AreEqual(2, cde.CurrentCount);
        Assert.IsFalse(cde.WaitAsync(default).IsCompleted);
    }
}