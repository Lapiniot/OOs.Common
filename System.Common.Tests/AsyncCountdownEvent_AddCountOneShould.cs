namespace System.Common.Tests;

[TestClass]
public class AsyncCountdownEvent_AddCountOneShould
{
    [TestMethod]
    public void ThrowInvalidOperationException_WhenEventAlreadySet() =>
        Assert.ThrowsException<InvalidOperationException>(() => new AsyncCountdownEvent(0).AddCount());

    [TestMethod]
    public void ThrowInvalidOperationException_GivenSignalsMoreThanMaxPossible() =>
        Assert.ThrowsException<InvalidOperationException>(() => new AsyncCountdownEvent(int.MaxValue).AddCount());

    [TestMethod]
    public void IncrementCurrentCountButDoNotSetEvent()
    {
        var cde = new AsyncCountdownEvent(1);

        cde.AddCount();

        Assert.AreEqual(2, cde.CurrentCount);
        Assert.IsFalse(cde.WaitAsync().IsCompleted);
    }
}