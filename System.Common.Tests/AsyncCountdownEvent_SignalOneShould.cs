namespace System.Common.Tests;

[TestClass]
public class AsyncCountdownEvent_SignalOneShould
{
    [TestMethod]
    public void ThrowInvalidOperationException_WhenEventAlreadySet() =>
        Assert.ThrowsException<InvalidOperationException>(() => new AsyncCountdownEvent(0).Signal());

    [TestMethod]
    public void DecrementCountButDoNotSetEvent_WhenCurrentCountIsGreaterThanOne()
    {
        var cde = new AsyncCountdownEvent(2);

        cde.Signal();

        Assert.AreEqual(1, cde.CurrentCount);
        Assert.IsFalse(cde.WaitAsync().IsCompleted);
    }

    [TestMethod]
    public void DecrementCountAndSetEvent_WhenCurrentCountEqualsOne()
    {
        var cde = new AsyncCountdownEvent(1);

        cde.Signal();

        Assert.AreEqual(0, cde.CurrentCount);
        Assert.IsTrue(cde.WaitAsync().IsCompletedSuccessfully);
    }
}