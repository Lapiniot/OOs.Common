namespace OOs.Common.Threading.Tests.AsyncCountdownEvent;

[TestClass]
public class SignalOneShould
{
    [TestMethod]
    public void ThrowInvalidOperationException_WhenEventAlreadySet() =>
        Assert.ThrowsExactly<InvalidOperationException>(() => new OOs.Threading.AsyncCountdownEvent(0).Signal());

    [TestMethod]
    public void DecrementCountButDoNotSetEvent_WhenCurrentCountIsGreaterThanOne()
    {
        var cde = new OOs.Threading.AsyncCountdownEvent(2);

        cde.Signal();

        Assert.AreEqual(1, cde.CurrentCount);
        Assert.IsFalse(cde.WaitAsync(default).IsCompleted);
    }

    [TestMethod]
    public void DecrementCountAndSetEvent_WhenCurrentCountEqualsOne()
    {
        var cde = new OOs.Threading.AsyncCountdownEvent(1);

        cde.Signal();

        Assert.AreEqual(0, cde.CurrentCount);
        Assert.IsTrue(cde.WaitAsync(default).IsCompletedSuccessfully);
    }
}