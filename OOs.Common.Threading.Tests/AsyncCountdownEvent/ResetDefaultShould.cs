namespace OOs.Common.Threading.Tests.AsyncCountdownEvent;

[TestClass]
public class ResetDefaultShould
{
    [TestMethod]
    public void SetCurrentCountToInitialCount()
    {
        var cde = new OOs.Threading.AsyncCountdownEvent(2);
        cde.Signal();

        cde.Reset();

        Assert.AreEqual(2, cde.InitialCount);
        Assert.AreEqual(2, cde.CurrentCount);
    }

    [TestMethod]
    public void ResetEvent_IfWasAlreadySetBefore()
    {
        var cde = new OOs.Threading.AsyncCountdownEvent(1);
        cde.Signal();

        cde.Reset();

        Assert.IsFalse(cde.WaitAsync(default).IsCompleted);
    }

    [TestMethod]
    public void SetEventImmediately_WhenInitialCountZero()
    {
        var cde = new OOs.Threading.AsyncCountdownEvent(0);

        cde.Reset();

        Assert.IsTrue(cde.WaitAsync(default).IsCompletedSuccessfully);
    }
}