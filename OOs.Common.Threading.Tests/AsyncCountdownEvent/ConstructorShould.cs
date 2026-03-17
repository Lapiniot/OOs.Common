namespace OOs.Common.Threading.Tests.AsyncCountdownEvent;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void SetInitialCountAndCurrentCountToArgValue_GivenValidValue()
    {
        var cde = new OOs.Threading.AsyncCountdownEvent(5);

        Assert.AreEqual(5, cde.CurrentCount);
        Assert.AreEqual(5, cde.InitialCount);
    }

    [TestMethod]
    public void SetEventImmediately_GivenZeroValue()
    {
        var cde = new OOs.Threading.AsyncCountdownEvent(0);
        var task = cde.WaitAsync(default);

        Assert.IsTrue(task.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void NotSetEventImmediately_GivenPositiveValue()
    {
        var cde = new OOs.Threading.AsyncCountdownEvent(1);
        var task = cde.WaitAsync(default);

        Assert.IsFalse(task.IsCompleted);
    }

    [TestMethod]
    public void ThrowArgumentException_GivenNegativeValue() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new OOs.Threading.AsyncCountdownEvent(-1));
}