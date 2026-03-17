namespace OOs.Common.Threading.Tests.AsyncManualResetEvent;

[TestClass]
public class ResetShould
{
    [TestMethod]
    public void SetEventNonSignaled_FromSignaled()
    {
        var mre = new OOs.Threading.AsyncManualResetEvent(true);

        mre.Reset();

        Assert.IsFalse(mre.WaitAsync(default).IsCompleted);
    }

    [TestMethod]
    public void SubsequentCallsDontChangeState_AndDontThrow_WhenNonSignaled()
    {
        var mre = new OOs.Threading.AsyncManualResetEvent(false);

        mre.Reset();
        Assert.IsFalse(mre.WaitAsync(default).IsCompleted);

        mre.Reset();
        Assert.IsFalse(mre.WaitAsync(default).IsCompleted);

        mre.Reset();
        Assert.IsFalse(mre.WaitAsync(default).IsCompleted);
    }
}