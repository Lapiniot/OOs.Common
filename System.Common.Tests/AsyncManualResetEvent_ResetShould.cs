namespace System.Common.Tests;

[TestClass]
public class AsyncManualResetEvent_ResetShould
{
    [TestMethod]
    public void SetEventNonSignaled_FromSignaled()
    {
        var mre = new AsyncManualResetEvent(true);

        mre.Reset();

        Assert.IsFalse(mre.WaitAsync(default).IsCompleted);
    }

    [TestMethod]
    public void SubsequentCallsDontChangeState_AndDontThrow_WhenNonSignaled()
    {
        var mre = new AsyncManualResetEvent(false);

        mre.Reset();
        Assert.IsFalse(mre.WaitAsync(default).IsCompleted);

        mre.Reset();
        Assert.IsFalse(mre.WaitAsync(default).IsCompleted);

        mre.Reset();
        Assert.IsFalse(mre.WaitAsync(default).IsCompleted);
    }
}