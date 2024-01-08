namespace OOs.Common.Tests.AsyncManualResetEvent;

[TestClass]
public class SetShould
{
    [TestMethod]
    public async Task SetEventSignaled_FromNonSignaled()
    {
        var mre = new Threading.AsyncManualResetEvent(false);

        mre.Set();

        var task = mre.WaitAsync(default);
        await task.ConfigureAwait(false);
        Assert.IsTrue(task.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task SubsequentCallsDontChangeState_AndDontThrow_WhenAlreadySignaled()
    {
        var mre = new Threading.AsyncManualResetEvent(true);

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