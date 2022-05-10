namespace System.Common.Tests;

[TestClass]
public class AsyncCountdownEvent_ResetDefaultShould
{
    [TestMethod]
    public void SetCurrentCountToInitialCount()
    {
        var cde = new AsyncCountdownEvent(2);
        cde.Signal();

        cde.Reset();

        Assert.AreEqual(2, cde.InitialCount);
        Assert.AreEqual(2, cde.CurrentCount);
    }

    [TestMethod]
    public void ResetEvent_IfWasAlreadySetBefore()
    {
        var cde = new AsyncCountdownEvent(1);
        cde.Signal();

        cde.Reset();

        Assert.IsFalse(cde.WaitAsync().IsCompleted);
    }

    [TestMethod]
    public void SetEventImmidiately_WhenInitialCountZero()
    {
        var cde = new AsyncCountdownEvent(0);

        cde.Reset();

        Assert.IsTrue(cde.WaitAsync().IsCompletedSuccessfully);
    }
}