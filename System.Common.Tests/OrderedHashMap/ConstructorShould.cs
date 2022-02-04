namespace System.Common.Tests.OrderedHashMap;

[TestClass]
public class ConstructorShould
{
    [TestMethod]
    public void ThrowArgumentNullExceptionGivenNullCollection()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            _ = new OrderedHashMap<string, string>(null);
        });
    }

    [TestMethod]
    public void ThrowArgumentOutOfRangeExceptionGivenNegativeCapacity()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
        {
            _ = new OrderedHashMap<string, string>(-1);
        });
    }

    [TestMethod]
    public void AddItemsWithGivenCollectionOrder()
    {
        using var map = new OrderedHashMap<int, string>(new KeyValuePair<int, string>[] { new(10, "10"), new(1, "1"), new(4, "4"), new(15, "15"), new(0, "0") });
        using var enumerator = map.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("10", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("1", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("4", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("15", enumerator.Current);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("0", enumerator.Current);

        Assert.IsFalse(enumerator.MoveNext());
    }
}