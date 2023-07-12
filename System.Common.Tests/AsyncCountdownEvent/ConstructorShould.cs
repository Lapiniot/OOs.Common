namespace System.Common.Tests.AsyncCountdownEvent;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void SetInitialCountAndCurrentCountToArgValue_GivenValidValue()
    {
        var cde = new Threading.AsyncCountdownEvent(5);

        Assert.AreEqual(5, cde.CurrentCount);
        Assert.AreEqual(5, cde.InitialCount);
    }

    [TestMethod]
    public void SetEventImmediately_GivenZeroValue()
    {
        var cde = new Threading.AsyncCountdownEvent(0);
        var task = cde.WaitAsync();

        Assert.IsTrue(task.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void NotSetEventImmediately_GivenPositiveValue()
    {
        var cde = new Threading.AsyncCountdownEvent(1);
        var task = cde.WaitAsync();

        Assert.IsFalse(task.IsCompleted);
    }

    [TestMethod]
    public void ThrowArgumentException_GivenNegativeValue() =>
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Threading.AsyncCountdownEvent(-1));
}