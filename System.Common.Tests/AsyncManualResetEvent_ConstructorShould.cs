namespace System.Common.Tests;

[TestClass]
public class AsyncManualResetEvent_ConstructorShould
{
    [TestMethod]
    public async Task SetInitialStateSignaled_GivenTrueParamValue()
    {
        var mre = new AsyncManualResetEvent(true);

        var task = mre.WaitAsync(CancellationToken.None);
        await task.ConfigureAwait(false);

        Assert.IsTrue(task.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void SetInitialStateNonSignaled_GivenFalseParamValue()
    {
        var mre = new AsyncManualResetEvent(false);

        var task = mre.WaitAsync(CancellationToken.None);

        Assert.IsFalse(task.IsCompleted);
    }

    [TestMethod]
    public void SetInitialStateNonSignaled_GivenDefaultParamValue()
    {
        var mre = new AsyncManualResetEvent();

        var task = mre.WaitAsync(CancellationToken.None);

        Assert.IsFalse(task.IsCompleted);
    }
}