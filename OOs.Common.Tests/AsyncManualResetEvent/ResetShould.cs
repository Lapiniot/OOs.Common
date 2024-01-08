namespace OOs.Common.Tests.AsyncManualResetEvent;

[TestClass]
public class ResetShould
{
    [TestMethod]
    public void SetEventNonSignaled_FromSignaled()
    {
        var mre = new Threading.AsyncManualResetEvent(true);

        mre.Reset();

        Assert.IsFalse(mre.WaitAsync(default).IsCompleted);
    }

    [TestMethod]
    public void SubsequentCallsDontChangeState_AndDontThrow_WhenNonSignaled()
    {
        var mre = new Threading.AsyncManualResetEvent(false);

        mre.Reset();
        Assert.IsFalse(mre.WaitAsync(default).IsCompleted);

        mre.Reset();
        Assert.IsFalse(mre.WaitAsync(default).IsCompleted);

        mre.Reset();
        Assert.IsFalse(mre.WaitAsync(default).IsCompleted);
    }
}