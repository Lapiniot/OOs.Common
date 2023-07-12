namespace System.Common.Tests.AsyncManualResetEvent;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public async Task SetInitialStateSignaled_GivenTrueParamValue()
    {
        var mre = new Threading.AsyncManualResetEvent(true);

        var task = mre.WaitAsync(CancellationToken.None);
        await task.ConfigureAwait(false);

        Assert.IsTrue(task.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void SetInitialStateNonSignaled_GivenFalseParamValue()
    {
        var mre = new Threading.AsyncManualResetEvent(false);

        var task = mre.WaitAsync(CancellationToken.None);

        Assert.IsFalse(task.IsCompleted);
    }

    [TestMethod]
    public void SetInitialStateNonSignaled_GivenDefaultParamValue()
    {
        var mre = new Threading.AsyncManualResetEvent();

        var task = mre.WaitAsync(CancellationToken.None);

        Assert.IsFalse(task.IsCompleted);
    }
}