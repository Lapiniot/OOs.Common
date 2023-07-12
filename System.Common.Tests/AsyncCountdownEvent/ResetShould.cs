namespace System.Common.Tests.AsyncCountdownEvent;

[TestClass]
public class ResetShould
{
    [TestMethod]
    public void ThrowArgumentOutOfRangeException_GivenNegativeValue() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Threading.AsyncCountdownEvent(1).Reset(-1));

    [TestMethod]
    public void SetEventImmediately_GivenZeroValue()
    {
        var cde = new Threading.AsyncCountdownEvent(1);

        cde.Reset(0);

        Assert.IsTrue(cde.WaitAsync().IsCompletedSuccessfully);
    }

    [TestMethod]
    public void SetCurrentCountAndInitialCount_GivenPositiveValue()
    {
        var cde = new Threading.AsyncCountdownEvent(1);

        cde.Reset(2);

        Assert.AreEqual(2, cde.InitialCount);
        Assert.AreEqual(2, cde.CurrentCount);
    }

    [TestMethod]
    public void DoNotSetEventImmediately_GivenPositiveValue()
    {
        var cde = new Threading.AsyncCountdownEvent(1);

        cde.Reset(2);

        Assert.IsFalse(cde.WaitAsync().IsCompleted);
    }

    [TestMethod]
    public void ResetEvent_GivenPositiveValue()
    {
        var cde = new Threading.AsyncCountdownEvent(0);

        cde.Reset(1);

        Assert.IsFalse(cde.WaitAsync().IsCompleted);
    }
}