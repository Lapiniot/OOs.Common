namespace OOs.Common.Threading.Tests.AsyncManualResetEvent;

[TestClass]
public class SetShould
{
    [TestMethod]
    public async Task SetEventSignaled_FromNonSignaled()
    {
        var mre = new OOs.Threading.AsyncManualResetEvent(false);

        mre.Set();

        var task = mre.WaitAsync(default);
        await task.ConfigureAwait(false);
        Assert.IsTrue(task.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task SubsequentCallsDontChangeState_AndDontThrow_WhenAlreadySignaled()
    {
        var mre = new OOs.Threading.AsyncManualResetEvent(true);

        mre.Set();
        var task = mre.WaitAsync(default);
        await task.ConfigureAwait(false);
        Assert.IsTrue(task.IsCompletedSuccessfully);

        mre.Set();
        task = mre.WaitAsync(default);
        await task.ConfigureAwait(false);
        Assert.IsTrue(task.IsCompletedSuccessfully);

        mre.Set();
        task = mre.WaitAsync(default);
        await task.ConfigureAwait(false);
        Assert.IsTrue(task.IsCompletedSuccessfully);
    }
}