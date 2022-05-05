namespace System.Common.Tests;

[TestClass]
public class AsyncCountdownEvent_ConstructorShould
{
    [TestMethod]
    public void SetInitialCountAndCurrentCountToArgValue_GivenValidValue()
    {
        var cde = new AsyncCountdownEvent(5);

        Assert.AreEqual(5, cde.CurrentCount);
        Assert.AreEqual(5, cde.InitialCount);
    }

    [TestMethod]
    public void SetCompleteState_GivenZeroValue()
    {
        var cde = new AsyncCountdownEvent(0);
        var task = cde.WaitAsync();

        Assert.IsTrue(task.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void SetIncompleteState_GivenPositiveValue()
    {
        var cde = new AsyncCountdownEvent(1);
        var task = cde.WaitAsync();

        Assert.IsFalse(task.IsCompleted);
    }

    [TestMethod]
    public void ThrowArgumentException_GivenNegativeValue() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new AsyncCountdownEvent(-1));
}